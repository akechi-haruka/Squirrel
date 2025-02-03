Squirrel
Copyright (C) 2021-2024 Haruka, Yonokid, SmashForge Authors
Licensed under the GPLv3.

---

A command-line .dds<->.nut converter and .lmz un/re-packer.
Specialized for two certain arcade games of B***** N****.

Usage:
Squirrel.exe [args] <input file> [output file]

Squirrel can unpack and pack .nut image files to .dds and repack them to .nut.
Files, Folders and .lmz files can directly be dragged onto the Squirrel program file or be specified in command line.

.png files will be converted to .dds.
.nut files will be converted to .dds.
.dds files will be converted back to .nut.
.lmz files will be unpacked and also convert any included .nut files directly.

For folders, if the folder is an extracted .lmz file, it will be re-packed to .lmz, including any .dds files back to .nut inside it. Otherwise all .nut files within the specified folder will be converted. 

If no output file is specified, files will be extracted into the folder of the input file. 

Arguments:

  -v, --verbose                                  Set output to verbose messages.
  -q, --quiet                                    Output nothing.
  --format=Rgb,Bgr,Argb,Rgba,...                 Override the pixel format of the input file.
  --intformat=R32i,R32ui,Rg8i,...                Override the internal pixel format of the input file.
  --compression=Optimal,Fastest,NoCompression    Set compression for LMZ files.
  --endian=Big,Little                            Set endian for NUT files.
  --output-png                                   For NUT files, output png files instead of dds files.
  --multi-nut                                    (does not work) For NUT files, if multiple dds files are given, convert
                                                 them into one .nut rather than one .nut per .dds. (also applies to LMZ
                                                 repacking)
  --help                                         Display this help screen.
  --version                                      Display version information.
  Input File (pos. 0)                            Required. The file to convert
  Output File / Folder (pos. 1)                  The file or folder to output or blank for the current directory
