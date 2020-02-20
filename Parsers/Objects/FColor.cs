using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FColor : IUStruct
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;

        internal FColor(BinaryReader reader)
        {
            R = reader.ReadByte();
            G = reader.ReadByte();
            B = reader.ReadByte();
            A = reader.ReadByte();
        }
    }
}
