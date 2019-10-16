using System.IO;

namespace PakReader.Parsers.Objects
{
    public class FCustomVersion
    {
        public FGuid Key;
        public int Version;
        // public int ReferenceCount; unused in serialization

        public FCustomVersion(BinaryReader reader)
        {
            Key = new FGuid(reader);
            Version = reader.ReadInt32();
        }
    }
}
