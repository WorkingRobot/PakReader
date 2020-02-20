using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class Int64Property : BaseProperty<long>
    {
        internal Int64Property(PackageReader reader, FPropertyTag tag)
        {
            Value = reader.ReadInt64();
        }
    }
}
