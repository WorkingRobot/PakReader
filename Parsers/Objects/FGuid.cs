using System.IO;

namespace PakReader.Parsers.Objects
{
    public struct FGuid
    {
        public uint A;
        public uint B;
        public uint C;
        public uint D;

        public FGuid(BinaryReader reader)
        {
            A = reader.ReadUInt32();
            B = reader.ReadUInt32();
            C = reader.ReadUInt32();
            D = reader.ReadUInt32();
        }
    }
}
