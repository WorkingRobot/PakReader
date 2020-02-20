using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class FloatProperty : BaseProperty<float>
    {
        internal FloatProperty(PackageReader reader, FPropertyTag tag)
        {
            Value = reader.ReadFloat();
        }
    }
}
