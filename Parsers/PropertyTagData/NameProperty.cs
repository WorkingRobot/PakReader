using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class NameProperty : BaseProperty<FName>
    {
        internal NameProperty(PackageReader reader, FPropertyTag tag)
        {
            Value = reader.ReadFName();
        }
    }
}
