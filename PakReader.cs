using System;
using System.Collections.Generic;
using System.IO;

namespace PakReader
{
    public class PakReader
    {
        public readonly Stream Stream;
        public readonly BinaryReader Reader;
        public readonly byte[] Aes;
        public readonly string MountPoint;
        public readonly string[] FileNames;
        public readonly FPakEntry[] FileInfos;
        public readonly string Name;

        public PakReader(string file, byte[] aes = null) : this(file, new byte[][] { aes }, out _) { }
        public PakReader(Stream stream, string name, byte[] aes = null) : this(stream, name, new byte[][] { aes }, out _) { }

        public PakReader(string file, IList<byte[]> aes, out int aesInd) : this(File.OpenRead(file), file, aes, out aesInd) { }

        public PakReader(Stream stream, string name, IList<byte[]> aes, out int aesInd)
        {
            Stream = stream;
            Name = name;
            Reader = new BinaryReader(Stream);

            Stream.Seek(-FPakInfo.Size, SeekOrigin.End);

            FPakInfo info = new FPakInfo(Reader);
            if (info.Magic != FPakInfo.PAK_FILE_MAGIC)
            {
                throw new FileLoadException("The file magic is invalid");
            }

            if (info.Version > (int)PAK_VERSION.PAK_LATEST)
            {
                Console.Error.WriteLine($"WARNING: Pak file \"{Name}\" has unsupported version {info.Version}");
            }

            // Read pak index

            Stream.Position = info.IndexOffset;

            // Manage pak files with encrypted index
            var infoReader = Reader;

            if (info.bEncryptedIndex != 0)
            {
                if (aes == null)
                {
                    throw new FileLoadException("The file has an encrypted index");
                }
                var indexBlock = Reader.ReadBytes((int)info.IndexSize);
                aesInd = AESDecryptor.FindKey(indexBlock, aes);
                if (aesInd == -1)
                    throw new FileLoadException("All AES keys are invalid");
                Aes = aes[aesInd];
                infoReader = new BinaryReader(new MemoryStream(AESDecryptor.DecryptAES(indexBlock, Aes)));
            }
            else
                aesInd = -1;

            // Pak index reading time :)
            infoReader.BaseStream.Position = 0;
            MountPoint = infoReader.ReadFString(FPakInfo.MAX_PACKAGE_PATH);

            bool badMountPoint = false;
            if (!MountPoint.StartsWith("../../.."))
            {
                badMountPoint = true;
            }
            else
            {
                MountPoint = MountPoint.Substring(8);
            }
            if (MountPoint[0] != '/' || ((MountPoint.Length > 1) && (MountPoint[1] == '.')))
            {
                badMountPoint = true;
            }

            if (badMountPoint)
            {
                Console.Error.WriteLine($"WARNING: Pak \"{Name}\" has strange mount point \"{MountPoint}\", mounting to root");
                MountPoint = "/";
            }
            
            FileInfos = new FPakEntry[infoReader.ReadInt32()];
            FileNames = new string[FileInfos.Length];
            for (int i = 0; i < FileInfos.Length; i++)
            {
                FileInfos[i] = new FPakEntry(infoReader, info.Version, out FileNames[i]);
            }
        }
    }
}
