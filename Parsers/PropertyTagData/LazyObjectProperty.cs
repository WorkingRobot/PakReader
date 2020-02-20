using System;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class LazyObjectProperty : BaseProperty<object>
    {
        internal LazyObjectProperty(PackageReader reader, FPropertyTag tag)
        {
            // Let me know if you find a package that has a LazyObjectProperty
            throw new NotImplementedException("Parsing of LazyObjectProperty types aren't supported yet.");
        }
    }
}
