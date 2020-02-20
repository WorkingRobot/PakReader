using System;
using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FPakEntry
    {
        const byte Flag_None = 0x00;
        const byte Flag_Encrypted = 0x01;
        const byte Flag_Deleted = 0x02;

        public bool Encrypted => (Flags & Flag_Encrypted) != 0;
        public bool Deleted => (Flags & Flag_Deleted) != 0;

        public readonly long Offset;
        public readonly long Size;
        public readonly long UncompressedSize;
        public readonly byte[] Hash; // why isn't this an FShaHash?
        public readonly FPakCompressedBlock[] CompressionBlocks;
        public readonly uint CompressionBlockSize;
        public readonly uint CompressionMethodIndex;
        public readonly byte Flags;

        public readonly int StructSize;

        internal FPakEntry(BinaryReader reader, EPakVersion Version)
        {
            CompressionBlocks = null;
            CompressionBlockSize = 0;
            Flags = 0;

            var StartOffset = reader.BaseStream.Position;

            Offset = reader.ReadInt64();
            Size = reader.ReadInt64();
            UncompressedSize = reader.ReadInt64();
            if (Version < EPakVersion.FNAME_BASED_COMPRESSION_METHOD)
            {
                var LegacyCompressionMethod = reader.ReadInt32();
                if (LegacyCompressionMethod == (int)ECompressionFlags.COMPRESS_None)
                {
                    CompressionMethodIndex = 0;
                }
                else if ((LegacyCompressionMethod & (int)ECompressionFlags.COMPRESS_ZLIB) != 0)
                {
                    CompressionMethodIndex = 1;
                }
                else if ((LegacyCompressionMethod & (int)ECompressionFlags.COMPRESS_GZIP) != 0)
                {
                    CompressionMethodIndex = 2;
                }
                else if ((LegacyCompressionMethod & (int)ECompressionFlags.COMPRESS_Custom) != 0)
                {
                    CompressionMethodIndex = 3;
                }
                else
                {
                    // https://github.com/EpicGames/UnrealEngine/blob/8b6414ae4bca5f93b878afadcc41ab518b09984f/Engine/Source/Runtime/PakFile/Public/IPlatformFilePak.h#L441
                    throw new FileLoadException(@"Found an unknown compression type in pak file, will need to be supported for legacy files");
                }
            }
            else
            {
                CompressionMethodIndex = reader.ReadUInt32();
            }
            if (Version <= EPakVersion.INITIAL)
            {
                // Timestamp of type FDateTime, but the serializer only reads to the Ticks property (int64)
                reader.ReadInt64();
            }
            Hash = reader.ReadBytes(20);
            if (Version >= EPakVersion.COMPRESSION_ENCRYPTION)
            {
                if (CompressionMethodIndex != 0)
                {
                    CompressionBlocks = reader.ReadTArray(() => new FPakCompressedBlock(reader));
                }
                Flags = reader.ReadByte();
                CompressionBlockSize = reader.ReadUInt32();
            }

            // Used to seek ahead to the file data instead of parsing the entry again
            StructSize = (int)(reader.BaseStream.Position - StartOffset);
        }

        internal FPakEntry(BinaryReader reader)
        {
            CompressionBlocks = null;
            CompressionBlockSize = 0;
            Flags = 0;

            var StartOffset = reader.BaseStream.Position;

            Offset = reader.ReadInt64();
            Size = reader.ReadInt64();
            UncompressedSize = reader.ReadInt64();
            CompressionMethodIndex = reader.ReadUInt32();
            Hash = reader.ReadBytes(20);
            if (CompressionMethodIndex != 0)
            {
                CompressionBlocks = reader.ReadTArray(() => new FPakCompressedBlock(reader));
            }
            Flags = reader.ReadByte();
            CompressionBlockSize = reader.ReadUInt32();

            // Used to seek ahead to the file data instead of parsing the entry again
            StructSize = (int)(reader.BaseStream.Position - StartOffset);
        }

        public ArraySegment<byte> GetData(Stream stream, byte[] key)
        {
            if (CompressionMethodIndex != 0)
                throw new NotImplementedException("Decompression not yet implemented");
            lock (stream)
            {
                stream.Position = Offset + StructSize;
                if (Encrypted)
                {
                    var data = new byte[(Size & 15) == 0 ? Size : ((Size / 16) + 1) * 16];
                    stream.Read(data);
                    return new ArraySegment<byte>(AESDecryptor.DecryptAES(data, key), 0, (int)UncompressedSize);
                }
                else
                {
                    var data = new byte[UncompressedSize];
                    stream.Read(data);
                    return new ArraySegment<byte>(data);
                }
            }
        }
    }
}
