using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class StrProperty : BaseProperty<string>
    {
        internal StrProperty(PackageReader reader, FPropertyTag tag)
        {
            Value = reader.ReadFString();
        }
    }
}
