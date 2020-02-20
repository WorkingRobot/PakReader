using System;
using System.IO;
using PakReader.Parsers;

namespace PakReader.Pak
{
    public readonly struct PakPackage
    {
        // might optimize this if I add more extensions like umaps or uptnls
        readonly ArraySegment<byte> UAsset;
        readonly ArraySegment<byte> UExp;
        readonly ArraySegment<byte> UBulk;

        public UObject[] Exports
        {
            get
            {
                if (exports.Exports == null)
                {
                    using var asset = new MemoryStream(UAsset.Array, UAsset.Offset, UAsset.Count);
                    using var exp = new MemoryStream(UExp.Array, UExp.Offset, UExp.Count);
                    using var bulk = UBulk != null ? new MemoryStream(UBulk.Array, UBulk.Offset, UBulk.Count) : null;
                    asset.Position = 0;
                    exp.Position = 0;
                    if (bulk != null)
                        bulk.Position = 0;
                    return exports.Exports = new PackageReader(asset, exp, bulk).Exports;
                }
                return exports.Exports;
            }
        }
        readonly ExportList exports;

        internal PakPackage(ArraySegment<byte> asset, ArraySegment<byte> exp, ArraySegment<byte> bulk)
        {
            UAsset = asset;
            UExp = exp;
            UBulk = bulk;
            exports = new ExportList();
        }

        public T GetExport<T>() where T : UObject
        {
            var exports = Exports;
            for (int i = 0; i < exports.Length; i++)
            {
                if (exports[i] is T)
                    return (T)exports[i];
            }
            return null;
        }

        // hacky way to get the package to be a readonly struct, essentially a double pointer i guess
        sealed class ExportList
        {
            public UObject[] Exports;
        }
    }
}
