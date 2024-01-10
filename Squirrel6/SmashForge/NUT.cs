﻿using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL;
using System.Windows.Forms;
using SFGraphics.GLObjects.Textures;
using SFGraphics.GLObjects.Textures.TextureFormats;
using Squirrel;

namespace SmashForge {
    public class TextureSurface {
        public List<byte[]> mipmaps = new List<byte[]>();
        public uint cubemapFace = 0; //Not set currently
    }

    public class NutTexture : TreeNode {
        //Each texture should contain either 1 or 6 surfaces
        //Each surface should contain (1 <= n <= 255) mipmaps
        //Each surface in a texture should have the same amount of mipmaps and dimensions for them

        public List<TextureSurface> surfaces = new List<TextureSurface>();

        public byte MipMapsPerSurface {
            get { return (byte)surfaces[0].mipmaps.Count; }
        }

        public int HashId {
            get {
                return id;
            }
            set {
                Text = value.ToString("x").ToUpper();
                id = value;
            }
        }
        private int id;

        // Loading mip maps is only supported for DDS currently.
        public bool isDds = false;

        public int Width;
        public int Height;

        public PixelInternalFormat pixelInternalFormat;
        public OpenTK.Graphics.OpenGL.PixelFormat pixelFormat;
        public PixelType pixelType = PixelType.UnsignedByte;

        public uint DdsCaps2 {
            get {
                if (surfaces.Count == 6)
                    return (uint)Dds.Ddscaps2.CubemapAllfaces;
                else
                    return (uint)0;
            }
        }

        //Return a list containing every mipmap from every surface
        public List<byte[]> GetAllMipmaps() {
            List<byte[]> mipmaps = new List<byte[]>();
            foreach (TextureSurface surface in surfaces) {
                foreach (byte[] mipmap in surface.mipmaps) {
                    mipmaps.Add(mipmap);
                }
            }
            return mipmaps;
        }

        //Move channel 0 to channel 3 (ABGR -> BGRA)
        public void SwapChannelOrderUp() {
            foreach (byte[] mip in GetAllMipmaps()) {
                for (int t = 0; t < mip.Length; t += 4) {
                    byte t1 = mip[t];
                    mip[t] = mip[t + 1];
                    mip[t + 1] = mip[t + 2];
                    mip[t + 2] = mip[t + 3];
                    mip[t + 3] = t1;
                }
            }
        }

        //Move channel 3 to channel 0 (BGRA -> ABGR)
        public void SwapChannelOrderDown() {
            foreach (byte[] mip in GetAllMipmaps()) {
                for (int t = 0; t < mip.Length; t += 4) {
                    byte t1 = mip[t + 3];
                    mip[t + 3] = mip[t + 2];
                    mip[t + 2] = mip[t + 1];
                    mip[t + 1] = mip[t];
                    mip[t] = t1;
                }
            }
        }

        public NutTexture() {
            ImageKey = "texture";
            SelectedImageKey = "texture";
        }

        public override string ToString() {
            return HashId.ToString("x").ToUpper();
        }

        public int ImageSize {
            get {
                switch (pixelInternalFormat) {
                    case PixelInternalFormat.CompressedRedRgtc1:
                    case PixelInternalFormat.CompressedRgbaS3tcDxt1Ext:
                        return (Width * Height / 2);
                    case PixelInternalFormat.CompressedRgRgtc2:
                    case PixelInternalFormat.CompressedRgbaS3tcDxt3Ext:
                    case PixelInternalFormat.CompressedRgbaS3tcDxt5Ext:
                        return (Width * Height);
                    case PixelInternalFormat.Rgba16:
                        return surfaces[0].mipmaps[0].Length / 2;
                    case PixelInternalFormat.Rgba:
                        return surfaces[0].mipmaps[0].Length;
                    default:
                        return surfaces[0].mipmaps[0].Length;
                }
            }
        }

        public int getNutFormat() {
            Program.WriteLineIfVerbose("getNutFormat: " + pixelInternalFormat);
            switch (pixelInternalFormat) {
                case PixelInternalFormat.CompressedRgbaS3tcDxt1Ext:
                    return 0;
                case PixelInternalFormat.CompressedRgbaS3tcDxt3Ext:
                    return 1;
                case PixelInternalFormat.CompressedRgbaS3tcDxt5Ext:
                    return 2;
                case PixelInternalFormat.Rgb16:
                    return 8;
                case PixelInternalFormat.CompressedRedRgtc1:
                    return 21;
                case PixelInternalFormat.CompressedRgRgtc2:
                    return 22;
                case PixelInternalFormat.Rgba:
                    return 17;
                case PixelInternalFormat.Rgb32f:
                case PixelInternalFormat.Rgb32i:
                case PixelInternalFormat.Rgb32ui:
                    return 21;
                default:
                    throw new NotImplementedException($"Unknown pixel format 0x{pixelInternalFormat:X}");
            }
        }

        public void setPixelFormatFromNutFormat(int typet) {
            Program.WriteLineIfVerbose("Pixel format according to NUT is: " + typet);
            switch (typet) {
                case 0x0:
                    pixelInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
                    break;
                case 0x1:
                    pixelInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
                    break;
                case 0x2:
                    pixelInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
                    break;
                case 8:
                    pixelInternalFormat = PixelInternalFormat.Rgb16;
                    pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Rgb;
                    pixelType = PixelType.UnsignedShort565Reversed;
                    break;
                case 12:
                    pixelInternalFormat = PixelInternalFormat.Rgba16;
                    pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Rgba;
                    break;
                case 14:
                    pixelInternalFormat = PixelInternalFormat.Rgba;
                    pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Rgba;
                    break;
                case 16:
                    pixelInternalFormat = PixelInternalFormat.Rgba;
                    pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.AbgrExt;
                    break;
                case 17:
                    pixelInternalFormat = PixelInternalFormat.Rgba;
                    pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Rgba;
                    break;
                case 21:
                // WRONG
                //pixelInternalFormat = PixelInternalFormat.CompressedRedRgtc1;
                    pixelInternalFormat = PixelInternalFormat.Rgb32ui;
                    pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Rgba;
                    break;
                case 22:
                    pixelInternalFormat = PixelInternalFormat.CompressedRgRgtc2;
                    break;
                default:
                    throw new NotImplementedException($"Unknown nut texture format 0x{typet:X}");
            }
            Program.WriteLineIfVerbose("which is " + pixelFormat + " / " + pixelInternalFormat);
        }
    }

    public class NUT : FileBase {
        // Dictionary<hash ID, Texture>
        public Dictionary<int, Texture> glTexByHashId = new Dictionary<int, Texture>();

        public ushort Version = 0x0200;

        public override Endianness Endian { get; set; }

        public NUT() {
            Text = "model.nut";
            ImageKey = "nut";
            SelectedImageKey = "nut";
        }

        public NUT(string filename) : this() {
            Read(filename);
        }

        public NUT(FileData d) : this() {
            Read(d);
        }

        public bool getTextureByID(int hash, out NutTexture suc) {
            suc = null;
            foreach (NutTexture t in Nodes)
                if (t.HashId == hash) {
                    suc = t;
                    return true;
                }

            return false;
        }

        #region Functions

        public void ConvertToDdsNut(bool regenerateMipMaps = true) {
            for (int i = 0; i < Nodes.Count; i++) {
                NutTexture originalTexture = (NutTexture)Nodes[i];

                // Reading/writing mipmaps is only supported for DDS textures,
                // so we will need to convert all the textures.
                Dds dds = new Dds(originalTexture);
                NutTexture ddsTexture = dds.ToNutTexture();
                ddsTexture.HashId = originalTexture.HashId;

                if (regenerateMipMaps)
                    RegenerateMipmapsFromTexture2D(ddsTexture);

                Nodes[i] = ddsTexture;
            }
        }

        public void Save(object sender, EventArgs args) {
            using (var sfd = new SaveFileDialog()) {
                sfd.Filter = "Namco Universal Texture (.nut)|*.nut|" +
                             "All Files (*.*)|*.*";

                if (sfd.ShowDialog() == DialogResult.OK) {
                    File.WriteAllBytes(sfd.FileName, Rebuild());
                }
            }
        }
        #endregion

        public override byte[] Rebuild() {
            FileOutput o = new FileOutput();
            FileOutput data = new FileOutput();

            //We always want BE for the first six bytes
            o.endian = Endianness.Big;
            data.endian = Endianness.Big;

            if (Endian == Endianness.Big) {
                o.WriteUInt(0x4E545033); //NTP3
            } else if (Endian == Endianness.Little) {
                o.WriteUInt(0x4E545744); //NTWD
            }

            //Most NTWU NUTs are 0x020E, which isn't valid for NTP3/NTWD
            if (Version > 0x0200)
                Version = 0x0200;
            o.WriteUShort(Version);

            //After that, endian is used appropriately
            o.endian = Endian;
            data.endian = Endian;

            o.WriteUShort((ushort)Nodes.Count);
            o.WriteInt(0);
            o.WriteInt(0);

            //calculate total header size
            uint headerLength = 0;

            foreach (NutTexture texture in Nodes) {
                byte surfaceCount = (byte)texture.surfaces.Count;
                bool isCubemap = surfaceCount == 6;
                if (surfaceCount < 1 || surfaceCount > 6)
                    throw new NotImplementedException($"Unsupported surface amount {surfaceCount} for texture with hash 0x{texture.HashId:X}. 1 to 6 faces are required.");
                else if (surfaceCount > 1 && surfaceCount < 6)
                    throw new NotImplementedException($"Unsupported cubemap face amount for texture with hash 0x{texture.HashId:X}. Six faces are required.");
                byte mipmapCount = (byte)texture.surfaces[0].mipmaps.Count;

                ushort headerSize = 0x50;
                if (isCubemap) {
                    headerSize += 0x10;
                }
                if (mipmapCount > 1) {
                    headerSize += (ushort)(mipmapCount * 4);
                    while (headerSize % 0x10 != 0)
                        headerSize += 1;
                }

                headerLength += headerSize;
            }

            // write headers+data
            foreach (NutTexture texture in Nodes) {
                byte surfaceCount = (byte)texture.surfaces.Count;
                bool isCubemap = surfaceCount == 6;
                byte mipmapCount = (byte)texture.surfaces[0].mipmaps.Count;

                uint dataSize = 0;

                foreach (var mip in texture.GetAllMipmaps()) {
                    dataSize += (uint)mip.Length;
                    while (dataSize % 0x10 != 0)
                        dataSize += 1;
                }

                ushort headerSize = 0x50;
                if (isCubemap) {
                    headerSize += 0x10;
                }
                if (mipmapCount > 1) {
                    headerSize += (ushort)(mipmapCount * 4);
                    while (headerSize % 0x10 != 0)
                        headerSize += 1;
                }

                o.WriteUInt(dataSize + headerSize);
                o.WriteUInt(0);
                o.WriteUInt(dataSize);
                o.WriteUShort(headerSize);
                o.WriteUShort(0);

                o.WriteByte(0);
                o.WriteByte(mipmapCount);
                o.WriteByte(0);
                o.WriteByte(texture.getNutFormat());
                o.WriteShort(texture.Width);
                o.WriteShort(texture.Height);
                o.WriteInt(0);
                o.WriteUInt(texture.DdsCaps2);

                if (Version < 0x0200) {
                    o.WriteUInt(0);
                } else if (Version >= 0x0200) {
                    o.WriteUInt((uint)(headerLength + data.Size()));
                }
                headerLength -= headerSize;
                o.WriteInt(0);
                o.WriteInt(0);
                o.WriteInt(0);

                if (isCubemap) {
                    o.WriteInt(texture.surfaces[0].mipmaps[0].Length);
                    o.WriteInt(texture.surfaces[0].mipmaps[0].Length);
                    o.WriteInt(0);
                    o.WriteInt(0);
                }

                if (texture.getNutFormat() == 14 || texture.getNutFormat() == 17) {
                    texture.SwapChannelOrderDown();
                }

                for (byte surfaceLevel = 0; surfaceLevel < surfaceCount; ++surfaceLevel) {
                    for (byte mipLevel = 0; mipLevel < mipmapCount; ++mipLevel) {
                        int ds = data.Size();
                        data.WriteBytes(texture.surfaces[surfaceLevel].mipmaps[mipLevel]);
                        data.Align(0x10);
                        if (mipmapCount > 1 && surfaceLevel == 0)
                            o.WriteInt(data.Size() - ds);
                    }
                }
                o.Align(0x10);

                if (texture.getNutFormat() == 14 || texture.getNutFormat() == 17) {
                    texture.SwapChannelOrderUp();
                }

                o.WriteBytes(new byte[] { 0x65, 0x58, 0x74, 0x00 }); // "eXt\0"
                o.WriteInt(0x20);
                o.WriteInt(0x10);
                o.WriteInt(0x00);

                o.WriteBytes(new byte[] { 0x47, 0x49, 0x44, 0x58 }); // "GIDX"
                o.WriteInt(0x10);
                o.WriteInt(texture.HashId);
                o.WriteInt(0);

                if (Version < 0x0200) {
                    o.WriteOutput(data);
                    data = new FileOutput();
                }
            }

            if (Version >= 0x0200)
                o.WriteOutput(data);

            return o.GetBytes();
        }

        public override void Read(string filename) {
            Read(new FileData(filename));
        }

        public void Read(FileData d) {
            Endian = Endianness.Big;
            d.endian = Endian;

            uint magic = d.ReadUInt();
            Version = d.ReadUShort();

            Program.WriteLineIfVerbose("NUT VERSION: " + Version);

            if (magic == 0x4E545033) //NTP3
            {
                Program.WriteLineIfVerbose("NUT TYPE: NTP3");
                ReadNTP3(d);
            } else if (magic == 0x4E545755) //NTWU
              {
                Program.WriteLineIfVerbose("NUT TYPE: NTWU");
                ReadNTWU(d);
            } else if (magic == 0x4E545744) //NTWD
              {
                Program.WriteLineIfVerbose("NUT TYPE: NTWD");
                Endian = Endianness.Little;
                d.endian = Endian;
                ReadNTP3(d);
            }
        }

        public void ReadNTP3(FileData d) {
            d.Seek(0x6);

            ushort count = d.ReadUShort();
            Program.WriteLineIfVerbose("NUT COUNT: " + count);

            d.Skip(0x8);
            int headerPtr = 0x10;

            for (ushort i = 0; i < count; ++i) {
                d.Seek(headerPtr);

                NutTexture tex = new NutTexture();
                tex.isDds = true;
                tex.pixelInternalFormat = PixelInternalFormat.Rgba32ui;

                int totalSize = d.ReadInt();
                d.Skip(4);
                int dataSize = d.ReadInt();
                int headerSize = d.ReadUShort();
                d.Skip(2);

                //It might seem that mipmapCount and pixelFormat would be shorts, but they're bytes because they stay in the same place regardless of endianness
                d.Skip(1);
                byte mipmapCount = d.ReadByte();
                d.Skip(1);
                tex.setPixelFormatFromNutFormat(d.ReadByte());
                tex.Width = d.ReadUShort();
                tex.Height = d.ReadUShort();
                d.Skip(4); //0 in dds nuts (like NTP3) and 1 in gtx nuts; texture type?
                uint caps2 = d.ReadUInt();

                bool isCubemap = false;
                byte surfaceCount = 1;
                if ((caps2 & (uint)Dds.Ddscaps2.Cubemap) == (uint)Dds.Ddscaps2.Cubemap) {
                    //Only supporting all six faces
                    if ((caps2 & (uint)Dds.Ddscaps2.CubemapAllfaces) == (uint)Dds.Ddscaps2.CubemapAllfaces) {
                        isCubemap = true;
                        surfaceCount = 6;
                    } else {
                        throw new NotImplementedException($"Unsupported cubemap face amount for texture {i} with hash 0x{tex.HashId:X}. Six faces are required.");
                    }
                }

                int dataOffset = 0;
                if (Version < 0x0200) {
                    dataOffset = headerPtr + headerSize;
                    d.ReadInt();
                } else if (Version >= 0x0200) {
                    dataOffset = d.ReadInt() + headerPtr;
                }
                d.ReadInt();
                d.ReadInt();
                d.ReadInt();

                //The size of a single cubemap face (discounting mipmaps). I don't know why it is repeated. If mipmaps are present, this is also specified in the mipSize section anyway.
                int cmapSize1 = 0;
                int cmapSize2 = 0;
                if (isCubemap) {
                    cmapSize1 = d.ReadInt();
                    cmapSize2 = d.ReadInt();
                    d.Skip(8);
                }

                int[] mipSizes = new int[mipmapCount];
                if (mipmapCount == 1) {
                    if (isCubemap)
                        mipSizes[0] = cmapSize1;
                    else
                        mipSizes[0] = dataSize;
                } else {
                    for (byte mipLevel = 0; mipLevel < mipmapCount; ++mipLevel) {
                        mipSizes[mipLevel] = d.ReadInt();
                    }
                    d.Align(0x10);
                }

                d.Skip(0x10); //eXt data - always the same

                d.Skip(4); //GIDX
                d.ReadInt(); //Always 0x10
                tex.HashId = d.ReadInt();
                d.Skip(4); // padding align 8

                for (byte surfaceLevel = 0; surfaceLevel < surfaceCount; ++surfaceLevel) {
                    TextureSurface surface = new TextureSurface();
                    for (byte mipLevel = 0; mipLevel < mipmapCount; ++mipLevel) {
                        byte[] texArray = d.GetSection(dataOffset, mipSizes[mipLevel]);
                        surface.mipmaps.Add(texArray);
                        dataOffset += mipSizes[mipLevel];
                    }
                    tex.surfaces.Add(surface);
                }

                if (tex.getNutFormat() == 14 || tex.getNutFormat() == 17) {
                    tex.SwapChannelOrderUp();
                }

                if (Version < 0x0200)
                    headerPtr += totalSize;
                else if (Version >= 0x0200)
                    headerPtr += headerSize;

                Nodes.Add(tex);
            }
        }

        public void ReadNTWU(FileData d) {
            d.Seek(0x6);

            ushort count = d.ReadUShort();

            d.Skip(0x8);
            int headerPtr = 0x10;

            for (ushort i = 0; i < count; ++i) {
                d.Seek(headerPtr);

                NutTexture tex = new NutTexture();
                tex.pixelInternalFormat = PixelInternalFormat.Rgba32ui;

                int totalSize = d.ReadInt();
                d.Skip(4);
                int dataSize = d.ReadInt();
                int headerSize = d.ReadUShort();
                d.Skip(2);

                d.Skip(1);
                byte mipmapCount = d.ReadByte();
                d.Skip(1);
                tex.setPixelFormatFromNutFormat(d.ReadByte());
                tex.Width = d.ReadUShort();
                tex.Height = d.ReadUShort();
                d.ReadInt(); //Always 1?
                uint caps2 = d.ReadUInt();

                bool isCubemap = false;
                byte surfaceCount = 1;
                if ((caps2 & (uint)Dds.Ddscaps2.Cubemap) == (uint)Dds.Ddscaps2.Cubemap) {
                    //Only supporting all six faces
                    if ((caps2 & (uint)Dds.Ddscaps2.CubemapAllfaces) == (uint)Dds.Ddscaps2.CubemapAllfaces) {
                        isCubemap = true;
                        surfaceCount = 6;
                    } else {
                        throw new NotImplementedException($"Unsupported cubemap face amount for texture {i} with hash 0x{tex.HashId:X}. Six faces are required.");
                    }
                }

                int dataOffset = d.ReadInt() + headerPtr;
                int mipDataOffset = d.ReadInt() + headerPtr;
                int gtxHeaderOffset = d.ReadInt() + headerPtr;
                d.ReadInt();

                int cmapSize1 = 0;
                int cmapSize2 = 0;
                if (isCubemap) {
                    cmapSize1 = d.ReadInt();
                    cmapSize2 = d.ReadInt();
                    d.Skip(8);
                }

                int imageSize = 0; //Total size of first mipmap of every surface
                int mipSize = 0; //Total size of mipmaps other than the first of every surface
                if (mipmapCount == 1) {
                    if (isCubemap)
                        imageSize = cmapSize1;
                    else
                        imageSize = dataSize;
                } else {
                    imageSize = d.ReadInt();
                    mipSize = d.ReadInt();
                    d.Skip((mipmapCount - 2) * 4);
                    d.Align(0x10);
                }

                d.Skip(0x10); //eXt data - always the same

                d.Skip(4); //GIDX
                d.ReadInt(); //Always 0x10
                tex.HashId = d.ReadInt();
                d.Skip(4); // padding align 8

                d.Seek(gtxHeaderOffset);
                Gtx.Gx2Surface gtxHeader = new Gtx.Gx2Surface();

                gtxHeader.dim = d.ReadInt();
                gtxHeader.width = d.ReadInt();
                gtxHeader.height = d.ReadInt();
                gtxHeader.depth = d.ReadInt();
                gtxHeader.numMips = d.ReadInt();
                gtxHeader.format = d.ReadInt();
                gtxHeader.aa = d.ReadInt();
                gtxHeader.use = d.ReadInt();
                gtxHeader.imageSize = d.ReadInt();
                gtxHeader.imagePtr = d.ReadInt();
                gtxHeader.mipSize = d.ReadInt();
                gtxHeader.mipPtr = d.ReadInt();
                gtxHeader.tileMode = d.ReadInt();
                gtxHeader.swizzle = d.ReadInt();
                gtxHeader.alignment = d.ReadInt();
                gtxHeader.pitch = d.ReadInt();

                //mipOffsets[0] is not in this list and is simply the start of the data (dataOffset)
                //mipOffsets[1] is relative to the start of the data (dataOffset + mipOffsets[1])
                //Other mipOffsets are relative to mipOffset[1] (dataOffset + mipOffsets[1] + mipOffsets[i])
                int[] mipOffsets = new int[mipmapCount];
                mipOffsets[0] = 0;
                for (byte mipLevel = 1; mipLevel < mipmapCount; ++mipLevel) {
                    mipOffsets[mipLevel] = 0;
                    mipOffsets[mipLevel] = mipOffsets[1] + d.ReadInt();
                }

                for (byte surfaceLevel = 0; surfaceLevel < surfaceCount; ++surfaceLevel) {
                    tex.surfaces.Add(new TextureSurface());
                }

                int w = tex.Width, h = tex.Height;
                for (byte mipLevel = 0; mipLevel < mipmapCount; ++mipLevel) {
                    int p = gtxHeader.pitch / (gtxHeader.width / w);

                    int size;
                    if (mipmapCount == 1)
                        size = imageSize;
                    else if (mipLevel + 1 == mipmapCount)
                        size = (mipSize + mipOffsets[1]) - mipOffsets[mipLevel];
                    else
                        size = mipOffsets[mipLevel + 1] - mipOffsets[mipLevel];

                    size /= surfaceCount;

                    for (byte surfaceLevel = 0; surfaceLevel < surfaceCount; ++surfaceLevel) {
                        gtxHeader.data = d.GetSection(dataOffset + mipOffsets[mipLevel] + (size * surfaceLevel), size);

                        //Real size
                        //Leave the below line commented for now because it breaks RGBA textures
                        //size = ((w + 3) >> 2) * ((h + 3) >> 2) * (GTX.getBPP(gtxHeader.format) / 8);
                        if (size < (Gtx.GetBpp(gtxHeader.format) / 8))
                            size = (Gtx.GetBpp(gtxHeader.format) / 8);

                        byte[] deswiz = Gtx.SwizzleBc(
                            gtxHeader.data,
                            w,
                            h,
                            gtxHeader.format,
                            gtxHeader.tileMode,
                            p,
                            gtxHeader.swizzle
                        );
                        tex.surfaces[surfaceLevel].mipmaps.Add(new FileData(deswiz).GetSection(0, size));
                    }

                    w /= 2;
                    h /= 2;

                    if (w < 1)
                        w = 1;
                    if (h < 1)
                        h = 1;
                }

                headerPtr += headerSize;

                Nodes.Add(tex);
            }
        }

        public void RefreshGlTexturesByHashId() {
            glTexByHashId.Clear();

            foreach (NutTexture tex in Nodes) {
                if (!glTexByHashId.ContainsKey(tex.HashId)) {
                    // Check if the texture is a cube map.
                    if (tex.surfaces.Count == 6)
                        glTexByHashId.Add(tex.HashId, CreateTextureCubeMap(tex));
                    else
                        glTexByHashId.Add(tex.HashId, CreateTexture2D(tex));
                }
            }
        }

        public static void RegenerateMipmapsFromTexture2D(NutTexture tex) {
            if (!TextureFormatTools.IsCompressed(tex.pixelInternalFormat))
                return;

            //Rendering.OpenTkSharedResources.dummyResourceWindow.MakeCurrent();

            // Create an OpenGL texture with generated mipmaps.
            Texture2D texture2D = new Texture2D();
            texture2D.LoadImageData(tex.Width, tex.Height, tex.surfaces[0].mipmaps[0], (InternalFormat)tex.pixelInternalFormat);

            texture2D.Bind();

            for (int i = 0; i < tex.surfaces[0].mipmaps.Count; i++) {
                // Get the image size for the current mip level of the bound texture.
                int imageSize;
                GL.GetTexLevelParameter(TextureTarget.Texture2D, i,
                    GetTextureParameter.TextureCompressedImageSize, out imageSize);

                byte[] mipLevelData = new byte[imageSize];

                // Replace the Nut texture with the OpenGL texture's data.
                GL.GetCompressedTexImage(TextureTarget.Texture2D, i, mipLevelData);
                tex.surfaces[0].mipmaps[i] = mipLevelData;
            }
        }

        public void ChangeTextureIds(int newTexId) {
            // Check if tex ID fixing would cause any naming conflicts. 
            if (TexIdDuplicate4thByte()) {
                MessageBox.Show("The first six digits should be the same for all textures to prevent duplicate IDs after changing the Tex ID.",
                    "Duplicate Texture ID");
                return;
            }

            foreach (NutTexture tex in Nodes) {
                Texture originalTexture = glTexByHashId[tex.HashId];
                glTexByHashId.Remove(tex.HashId);

                // Only change the first 3 bytes.
                tex.HashId = tex.HashId & 0xFF;
                int first3Bytes = (int)(newTexId & 0xFFFFFF00);
                tex.HashId = tex.HashId | first3Bytes;

                glTexByHashId.Add(tex.HashId, originalTexture);
            }
        }

        public bool TexIdDuplicate4thByte() {
            // Check for duplicates. 
            List<byte> previous4thBytes = new List<byte>();
            foreach (NutTexture tex in Nodes) {
                byte fourthByte = (byte)(tex.HashId & 0xFF);
                if (!(previous4thBytes.Contains(fourthByte)))
                    previous4thBytes.Add(fourthByte);
                else
                    return true;

            }

            return false;
        }

        public override string ToString() {
            return "NUT";
        }

        public static Texture2D CreateTexture2D(NutTexture nutTexture, int surfaceIndex = 0) {
            bool compressedFormatWithMipMaps = TextureFormatTools.IsCompressed(nutTexture.pixelInternalFormat);

            List<byte[]> mipmaps = nutTexture.surfaces[surfaceIndex].mipmaps;

            if (compressedFormatWithMipMaps) {
                // HACK: Skip loading mipmaps for non square textures for now.
                // The existing mipmaps don't display properly for some reason.
                if (nutTexture.surfaces[0].mipmaps.Count > 1 && nutTexture.isDds && (nutTexture.Width == nutTexture.Height)) {
                    // Reading mipmaps past the first level is only supported for DDS currently.
                    Texture2D texture = new Texture2D();
                    texture.LoadImageData(nutTexture.Width, nutTexture.Height, nutTexture.surfaces[surfaceIndex].mipmaps,
                        (InternalFormat)nutTexture.pixelInternalFormat);
                    return texture;
                } else {
                    // Only load the first level and generate the rest.
                    Texture2D texture = new Texture2D();
                    texture.LoadImageData(nutTexture.Width, nutTexture.Height, mipmaps[0], (InternalFormat)nutTexture.pixelInternalFormat);
                    return texture;
                }
            } else {
                // Uncompressed.
                Texture2D texture = new Texture2D();
                texture.LoadImageData(nutTexture.Width, nutTexture.Height, mipmaps[0],
                    new TextureFormatUncompressed(nutTexture.pixelInternalFormat, nutTexture.pixelFormat, nutTexture.pixelType));
                return texture;
            }
        }

        public bool ContainsGtxTextures() {
            foreach (NutTexture texture in Nodes) {
                if (!texture.isDds)
                    return true;
            }
            return false;
        }

        public static TextureCubeMap CreateTextureCubeMap(NutTexture t) {
            if (TextureFormatTools.IsCompressed(t.pixelInternalFormat)) {
                // Compressed cubemap with mipmaps.
                TextureCubeMap texture = new TextureCubeMap();
                texture.LoadImageData(t.Width, (InternalFormat)t.pixelInternalFormat,
                    t.surfaces[0].mipmaps, t.surfaces[1].mipmaps, t.surfaces[2].mipmaps,
                    t.surfaces[3].mipmaps, t.surfaces[4].mipmaps, t.surfaces[5].mipmaps);
                return texture;
            } else {
                // Uncompressed cube map with no mipmaps.
                TextureCubeMap texture = new TextureCubeMap();
                texture.LoadImageData(t.Width, new TextureFormatUncompressed(t.pixelInternalFormat, t.pixelFormat, t.pixelType),
                    t.surfaces[0].mipmaps[0], t.surfaces[1].mipmaps[0], t.surfaces[2].mipmaps[0],
                    t.surfaces[3].mipmaps[0], t.surfaces[4].mipmaps[0], t.surfaces[5].mipmaps[0]);
                return texture;
            }
        }
    }
}
