using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class SoftObjectProperty : BaseProperty<FSoftObjectPath>
    {
        internal SoftObjectProperty(PackageReader reader, FPropertyTag tag, ReadType readType)
        {
            Value = new FSoftObjectPath(reader);
            if (readType == ReadType.MAP)
                reader.Position += 4; // skip ahead, putting the total bytes read to 16
        }
    }
}
