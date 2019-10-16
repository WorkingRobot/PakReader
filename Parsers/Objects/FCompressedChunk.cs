using System.IO;

namespace PakReader.Parsers.Objects
{
    struct FCompressedChunk
    {
        public int UncompressedOffset;
        public int UncompressedSize;
        public int CompressedOffset;
        public int CompressedSize;

        public FCompressedChunk(BinaryReader reader)
        {
            UncompressedOffset = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
            CompressedOffset = reader.ReadInt32();
            CompressedSize = reader.ReadInt32();
        }
    }
}
