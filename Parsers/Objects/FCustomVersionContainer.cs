using System.IO;

namespace PakReader.Parsers.Objects
{
    public class FCustomVersionContainer
    {
        public FCustomVersion[] Versions; // actually FCustomVersionArray, but typedeffed to TArray<FCustomVersion>

        public FCustomVersionContainer(BinaryReader reader)
        {
            Versions = reader.ReadTArray(() => new FCustomVersion(reader));
        }
    }
}
