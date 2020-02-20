using System;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class MulticastDelegateProperty : BaseProperty<object>
    {
        internal MulticastDelegateProperty(PackageReader reader, FPropertyTag tag)
        {
            // Let me know if you find a package that has a MutlicastDelegateProperty
            throw new NotImplementedException("Parsing of MulticastDelegateProperty types aren't supported yet.");
        }
    }
}
