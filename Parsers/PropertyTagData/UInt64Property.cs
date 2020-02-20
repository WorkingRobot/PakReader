using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class UInt64Property : BaseProperty<ulong>
    {
        internal UInt64Property(PackageReader reader, FPropertyTag tag)
        {
            Value = reader.ReadUInt64();
        }
    }
}
