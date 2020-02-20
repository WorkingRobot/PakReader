using System;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class DelegateProperty : BaseProperty
    {
        internal DelegateProperty(PackageReader reader, FPropertyTag tag)
        {
            // Let me know if you find a package that has a DelegateProperty
            throw new NotImplementedException("Parsing of DelegateProperty types aren't supported yet.");
        }
    }
}
