using System.IO;

namespace PakReader.Parsers.Objects
{
    public struct FEngineVersion
    {
        // FEngineVersionBase
        public ushort Major;
        public ushort Minor;
        public ushort Patch;
        public uint Changelist;

        // FEngineVersion
        public string Branch;

        public FEngineVersion(BinaryReader reader)
        {
            Major = reader.ReadUInt16();
            Minor = reader.ReadUInt16();
            Patch = reader.ReadUInt16();
            Changelist = reader.ReadUInt32();
            Branch = reader.ReadFString();
        }
    }
}
