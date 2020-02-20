using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class TextProperty : BaseProperty<FText>
    {
        internal TextProperty(PackageReader reader, FPropertyTag tag)
        {
            Value = new FText(reader);
        }
    }
}
