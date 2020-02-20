using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class StructProperty : BaseProperty<IUStruct>
    {
        internal StructProperty(PackageReader reader, FPropertyTag tag)
        {
            Value = new UScriptStruct(reader, tag.StructName).Struct;
        }
    }
}
