using SkiaSharp;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace PakReader
{
    public class PakIndex : IEnumerable<(string Path, PakPackage Package)>
    {
        ConcurrentDictionary<string, PakPackage> index = new ConcurrentDictionary<string, PakPackage>();

        static (string Path, string Extension) GetPath(string inp)
        {
            int extInd = inp.LastIndexOf('.');
            return (inp.Substring(0, extInd).ToLowerInvariant(), inp.Substring(extInd + 1).ToLowerInvariant());
        }

        static PakPackage InsertEntry(BasePakEntry entry, PakPackage package, string extension, PakReader reader)
        {
            switch (extension)
            {
                case "uasset":
                    package.uasset = entry;
                    package.AssetReader = reader;
                    break;
                case "uexp":
                    package.uexp = entry;
                    package.ExpReader = reader;
                    break;
                case "ubulk":
                    package.ubulk = entry;
                    package.BulkReader = reader;
                    break;
                case "ini":
                    package.ini = entry;
                    package.IniReader = reader;
                    break;
            }
            return package;
        }

        public void AddPak(string file, byte[] aes)
        {
            var reader = new PakReader(file, aes);
            foreach (var info in reader.FileInfos)
            {
                var path = GetPath(info.Name);
                if (!index.ContainsKey(path.Path))
                {
                    index[path.Path] = InsertEntry(info, new PakPackage(), path.Extension, reader);
                }
                else
                {
                    index[path.Path] = InsertEntry(info, index[path.Path], path.Extension, reader);
                }
            }
        }

        public PakPackage GetPackage(string name) => index.TryGetValue(name.ToLowerInvariant(), out PakPackage ret) ? ret : null;

        public IEnumerator<(string Path, PakPackage Package)> GetEnumerator()
        {
            foreach (var kv in index)
            {
                yield return (kv.Key, kv.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class PakPackage
    {
        public BasePakEntry uasset;
        public BasePakEntry uexp;
        public BasePakEntry ubulk;
        public BasePakEntry ini;

        public PakReader AssetReader;
        public PakReader ExpReader;
        public PakReader BulkReader;
        public PakReader IniReader;
        public ExportObject[] Exports
        {
            get
            {
                return new AssetReader(
                    AssetReader.GetPackageStream(uasset),
                    ExpReader.GetPackageStream(uexp),
                    ubulk == null ? null : BulkReader.GetPackageStream(ubulk)
                ).Exports;
            }
        }

        public void SaveFiles() // debugging purposes
        {
            using (var f = File.OpenWrite("out.uasset"))
                AssetReader.GetPackageStream(uasset).CopyTo(f);
            using (var f = File.OpenWrite("out.uexp"))
                ExpReader.GetPackageStream(uexp).CopyTo(f);
            if (ubulk != null)
                using (var f = File.OpenWrite("out.ubulk"))
                    BulkReader.GetPackageStream(ubulk).CopyTo(f);
        }

        public UObject GetUObject() => Exports[0] as UObject;

        public SKImage GetTexture()
        {
            if (Exports[0] is Texture2D tex)
            {
                return ImageExporter.GetImage(tex.textures[0].mips[0], tex.textures[0].pixel_format);
            }
            return null;
        }
    }
}
