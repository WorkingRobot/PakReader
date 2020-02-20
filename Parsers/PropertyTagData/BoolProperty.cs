using System;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class BoolProperty : BaseProperty<bool>
    {
        internal BoolProperty(PackageReader reader, FPropertyTag tag, ReadType readType)
        {
            Value = readType switch
            {
                ReadType.NORMAL => tag.BoolVal != 0,
                ReadType.MAP => reader.ReadByte() != 0,
                ReadType.ARRAY => reader.ReadByte() != 0,
                _ => throw new ArgumentOutOfRangeException(nameof(readType)),
            };
        }
    }
}
