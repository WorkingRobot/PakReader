using System.IO;

namespace PakReader.Parsers.Objects
{
    public struct FGenerationInfo
    {
        public int ExportCount;
        public int NameCount;

        public FGenerationInfo(BinaryReader reader)
        {
            ExportCount = reader.ReadInt32();
            NameCount = reader.ReadInt32();
        }
    }
}
