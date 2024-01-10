﻿using System;
using System.IO;
using System.IO.Compression;

namespace SmashForge
{
    public class FileData
    {
        private byte[] b;
        private int p = 0;
        public Endianness endian;

        public FileData(String f)
        {
            b = File.ReadAllBytes(f);

            // auto decompress
            if (b.Length > 2)
            {
                if (b[0] == 0x78 && b[1] == 0x9C)
                    b = InflateZlib(b);
            }
        }

        public FileData(byte[] b)
        {
            this.b = b;
        }

        public int Eof()
        {
            return b.Length;
        }

        public byte[] Read(int length)
        {
            if (length + p > b.Length)
                throw new IndexOutOfRangeException();

            var data = new byte[length];
            for (int i = 0; i < length; i++, p++)
            {
                data[i] = b[p];
            }
            return data;
        }

        public int ReadInt()
        {
            if (endian == Endianness.Little)
                return (int)((b[p++] & 0xFF) | ((b[p++] & 0xFF) << 8) | ((b[p++] & 0xFF) << 16) | ((b[p++] & 0xFF) << 24));
            else
                return (int)(((b[p++] & 0xFF) << 24) | ((b[p++] & 0xFF) << 16) | ((b[p++] & 0xFF) << 8) | (b[p++] & 0xFF));
        }

        public uint ReadUInt()
        {
            if (endian == Endianness.Little)
                return (uint)((b[p++] & 0xFF) | ((b[p++] & 0xFF) << 8) | ((b[p++] & 0xFF) << 16) | ((b[p++] & 0xFF) << 24));
            else
                return (uint)(((b[p++] & 0xFF) << 24) | ((b[p++] & 0xFF) << 16) | ((b[p++] & 0xFF) << 8) | (b[p++] & 0xFF));
        }

        public int ReadThree()
        {
            if (endian == Endianness.Little)
                return (b[p++] & 0xFF) | ((b[p++] & 0xFF) << 8) | ((b[p++] & 0xFF) << 16);
            else
                return ((b[p++] & 0xFF) << 16) | ((b[p++] & 0xFF) << 8) | (b[p++] & 0xFF);
        }

        public short ReadShort()
        {
            if (endian == Endianness.Little)
                return (short)((b[p++] & 0xFF) | ((b[p++] & 0xFF) << 8));
            else
                return (short)(((b[p++] & 0xFF) << 8) | (b[p++] & 0xFF));
        }

        public ushort ReadUShort()
        {
            if (endian == Endianness.Little)
                return (ushort)((b[p++] & 0xFF) | ((b[p++] & 0xFF) << 8));
            else
                return (ushort)(((b[p++] & 0xFF) << 8) | (b[p++] & 0xFF));
        }

        public byte ReadByte()
        {
            return (byte)(b[p++] & 0xFF);
        }

        public sbyte ReadSByte()
        {
            return (sbyte)(b[p++] & 0xFF);
        }

        public float ReadFloat()
        {
            byte[] by = new byte[4];
            if (endian == Endianness.Little)
                by = new byte[4] { b[p], b[p + 1], b[p + 2], b[p + 3] };
            else
                by = new byte[4] { b[p + 3], b[p + 2], b[p + 1], b[p] };
            p += 4;
            return BitConverter.ToSingle(by, 0);
        }

        public float ReadHalfFloat()
        {
            return ToFloat(ReadShort());
        }

        public static float ToFloat(int hbits)
        {
            int mant = hbits & 0x03ff;            // 10 bits mantissa
            int exp = hbits & 0x7c00;            // 5 bits exponent
            if (exp == 0x7c00)                   // NaN/Inf
                exp = 0x3fc00;                    // -> NaN/Inf
            else if (exp != 0)                   // normalized value
            {
                exp += 0x1c000;                   // exp - 15 + 127
                if (mant == 0 && exp > 0x1c400)  // smooth transition
                    return BitConverter.ToSingle(BitConverter.GetBytes((hbits & 0x8000) << 16
                        | exp << 13 | 0x3ff), 0);
            }
            else if (mant != 0)                  // && exp==0 -> subnormal
            {
                exp = 0x1c400;                    // make it normal
                do
                {
                    mant <<= 1;                   // mantissa * 2
                    exp -= 0x400;                 // decrease exp by 1
                } while ((mant & 0x400) == 0); // while not normal
                mant &= 0x3ff;                    // discard subnormal bit
            }                                     // else +/-0 -> +/-0
            return BitConverter.ToSingle(BitConverter.GetBytes(          // combine all parts
                (hbits & 0x8000) << 16          // sign  << ( 31 - 15 )
                | (exp | mant) << 13), 0);         // value << ( 23 - 10 )
        }

        public static int FromFloat(float fval)
        {
            int fbits = FileOutput.SingleToInt32Bits(fval);
            int sign = fbits >> 16 & 0x8000;          // sign only
            int val = (fbits & 0x7fffffff) + 0x1000; // rounded value

            if (val >= 0x47800000)               // might be or become NaN/Inf
            {                                     // avoid Inf due to rounding
                if ((fbits & 0x7fffffff) >= 0x47800000)
                {                                 // is or must become NaN/Inf
                    if (val < 0x7f800000)        // was value but too large
                        return sign | 0x7c00;     // make it +/-Inf
                    return sign | 0x7c00 |        // remains +/-Inf or NaN
                        (fbits & 0x007fffff) >> 13; // keep NaN (and Inf) bits
                }
                return sign | 0x7bff;             // unrounded not quite Inf
            }
            if (val >= 0x38800000)               // remains normalized value
                return sign | val - 0x38000000 >> 13; // exp - 127 + 15
            if (val < 0x33000000)                // too small for subnormal
                return sign;                      // becomes +/-0
            val = (fbits & 0x7fffffff) >> 23;  // tmp exp for subnormal calc
            return sign | ((fbits & 0x7fffff | 0x800000) // add subnormal bit
                + (0x800000 >> val - 102)     // round depending on cut off
                >> 126 - val);   // div by 2^(1-(exp-127+15)) and >> 13 | exp=0
        }

        public static int Sign12Bit(int i)
        {
            //        System.out.println(Integer.toBinaryString(i));
            if (((i >> 11) & 0x1) == 1)
            {
                //            System.out.println(i);
                i = ~i;
                i = i & 0xFFF;
                //            System.out.println(Integer.toBinaryString(i));
                //            System.out.println(i);
                i += 1;
                i *= -1;
            }

            return i;
        }


        public void Skip(int i)
        {
            p += i;
        }
        public void Seek(int i)
        {
            p = i;
        }

        public int Pos()
        {
            return p;
        }

        public int Size()
        {
            return b.Length;
        }

        public string ReadString()
        {
            string s = "";
            while (b[p] != 0x00)
            {
                s += (char)b[p];
                p++;
            }
            return s;
        }

        public byte[] GetSection(int offset, int size)
        {
            byte[] by = new byte[size];

            Array.Copy(b, offset, by, 0, size);

            return by;
        }

        public string ReadString(int p, int size)
        {
            if (size == -1)
            {
                String str = "";
                while (p < b.Length)
                {
                    if ((b[p] & 0xFF) != 0x00)
                        str += (char)(b[p] & 0xFF);
                    else
                        break;
                    p++;
                }
                return str;
            }

            string str2 = "";
            for (int i = p; i < p + size; i++)
            {
                str2 += (char)(b[i] & 0xFF);
                /*if ((b[i] & 0xFF) != 0x00)
                    str2 += (char)(b[i] & 0xFF);*/
            }
            return str2;
        }

        public void Align(int i)
        {
            while (p % i != 0)
                p++;
        }

        public void WriteInt(int value)
        {
            byte[] v = BitConverter.GetBytes(value);

            if (endian == Endianness.Little)
            {
                b[p++] = v[0]; b[p++] = v[1]; b[p++] = v[2]; b[p++] = v[3];
            }
            else
            {
                b[p++] = v[3]; b[p++] = v[2]; b[p++] = v[1]; b[p++] = v[0];
            }
        }

        public void WriteBytesAt(int p, byte[] bytes)
        {
            if(p + bytes.Length > b.Length)
            {
                byte[] newb = new byte[b.Length + ((p + bytes.Length + b.Length) - bytes.Length)];
                Array.Copy(b, newb, b.Length);
                b = newb;
            }
            for(int i =0; i < bytes.Length; i ++)
            {
                b[p++] = bytes[i];
            }
        }

        public void WriteInt(int pos, int value)
        {
            byte[] v = BitConverter.GetBytes(value);

            if (endian == Endianness.Little)
            {
                b[pos++] = v[0]; b[pos++] = v[1]; b[pos++] = v[2]; b[pos++] = v[3];
            }
            else
            {
                b[pos++] = v[3]; b[pos++] = v[2]; b[pos++] = v[1]; b[pos++] = v[0];
            }
        }

        public String Magic()
        {
            if (Size() < 4)
                return "";
            else
            {
                string m = "";
                if (Char.IsLetterOrDigit((char)b[0])) m += (char)b[0];
                if (Char.IsLetterOrDigit((char)b[1])) m += (char)b[1];
                if (Char.IsLetterOrDigit((char)b[2])) m += (char)b[2];
                if (Char.IsLetterOrDigit((char)b[3])) m += (char)b[3];

                return m;
            }
        }

        public static byte[] DeflateZlib(byte[] i)
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(0x78);
            output.WriteByte(0x9C);
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(i, 0, i.Length);
            }
            return output.ToArray();
        }

        public long ReadInt64()
        {
            if (endian == Endianness.Little)
            {
                return (b[p++] & 0xFF) | ((b[p++] & 0xFF) << 8) | ((b[p++] & 0xFF) << 16) | ((b[p++] & 0xFF) << 24) | ((b[p++] & 0xFF) << 32) | ((b[p++] & 0xFF) << 40) | ((b[p++] & 0xFF) << 48) | ((b[p++] & 0xFF) << 56);
            }
            else
                return ((b[p++] & 0xFF) << 56) | ((b[p++] & 0xFF) << 48) | ((b[p++] & 0xFF) << 40) | ((b[p++] & 0xFF) << 32) | ((b[p++] & 0xFF) << 24) | ((b[p++] & 0xFF) << 16) | ((b[p++] & 0xFF) << 8) | (b[p++] & 0xFF);
        }

        public static byte[] InflateZlib(byte[] i)
        {
            var stream = new MemoryStream();
            var ms = new MemoryStream(i);
            ms.ReadByte();
            ms.ReadByte();
            var zlibStream = new DeflateStream(ms, CompressionMode.Decompress);
            byte[] buffer = new byte[4095];
            while (true)
            {
                int size = zlibStream.Read(buffer, 0, buffer.Length);
                if (size > 0)
                    stream.Write(buffer, 0, buffer.Length);
                else
                    break;
            }
            zlibStream.Close();
            return stream.ToArray();
        }

        public class Decompress
        {
            public static FileData Yaz0(FileData i)
            {
                return new FileData(Yaz0(i.b));
            }

            private static byte[] Yaz0(byte[] data)
            {
                FileData f = new FileData(data);

                f.endian = Endianness.Big;
                f.Seek(4);
                int uncompressedSize = f.ReadInt();
                f.Seek(0x10);

                byte[] src = f.Read(data.Length - 0x10);
                byte[] dst = new byte[uncompressedSize];

                int srcPlace = 0, dstPlace = 0; //current read/write positions

                uint validBitCount = 0; //number of valid bits left in "code" byte
                byte currCodeByte = 0;
                while (dstPlace < uncompressedSize)
                {
                    //read new "code" byte if the current one is used up
                    if (validBitCount == 0)
                    {
                        currCodeByte = src[srcPlace];
                        ++srcPlace;
                        validBitCount = 8;
                    }

                    if ((currCodeByte & 0x80) != 0)
                    {
                        //straight copy
                        dst[dstPlace] = src[srcPlace];
                        dstPlace++;
                        srcPlace++;
                    }
                    else
                    {
                        //RLE part
                        byte byte1 = src[srcPlace];
                        byte byte2 = src[srcPlace + 1];
                        srcPlace += 2;

                        uint dist = (uint)(((byte1 & 0xF) << 8) | byte2);
                        uint copySource = (uint)(dstPlace - (dist + 1));

                        uint numBytes = (uint)(byte1 >> 4);
                        if (numBytes == 0)
                        {
                            numBytes = (uint)(src[srcPlace] + 0x12);
                            srcPlace++;
                        }
                        else
                            numBytes += 2;

                        //copy run
                        for (int i = 0; i < numBytes; ++i)
                        {
                            dst[dstPlace] = dst[copySource];
                            copySource++;
                            dstPlace++;
                        }
                    }

                    //use next bit from "code" byte
                    currCodeByte <<= 1;
                    validBitCount -= 1;
                }

                return dst;
            }
        }
    }
}
