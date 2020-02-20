using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FLinearColor : IUStruct
    {
        public readonly float R;
        public readonly float G;
        public readonly float B;
        public readonly float A;

        internal FLinearColor(BinaryReader reader)
        {
            R = reader.ReadSingle();
            G = reader.ReadSingle();
            B = reader.ReadSingle();
            A = reader.ReadSingle();
        }
    }
}
