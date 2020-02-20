using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class IntProperty : BaseProperty<int>
    {
        internal IntProperty(PackageReader reader, FPropertyTag tag)
        {
            Value = reader.ReadInt32();
        }
    }
}
