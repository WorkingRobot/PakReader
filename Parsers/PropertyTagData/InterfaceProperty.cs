using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class InterfaceProperty : BaseProperty<uint>
    {
        // Value is ObjectRef
        internal InterfaceProperty(PackageReader reader, FPropertyTag tag)
        {
            Value = reader.ReadUInt32();
        }
    }
}
