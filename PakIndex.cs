using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PakReader
{
    public class PakIndex : IDictionary<string, PakPackage>, IDictionary, IReadOnlyDictionary<string, PakPackage>
    {
        readonly ConcurrentDictionary<string, PakPackage> Index = new ConcurrentDictionary<string, PakPackage>();

        static (string Path, string Extension) GetPath(string inp)
        {
            int extInd = inp.LastIndexOf('.');
            return (inp.Substring(0, extInd).ToLowerInvariant(), inp.Substring(extInd + 1).ToLowerInvariant());
        }

        static PakPackage InsertEntry(FPakEntry entry, PakPackage package, string extension, PakReader reader)
        {
            package.Extensions.Add(new PakPackage.Extension(extension, reader.Aes, entry, reader.Stream));
            return package;
        }

        public void AddPak(string file, byte[] aes = null) => AddPak(new PakReader(file, aes));
        public void AddPak(Stream stream, string name, byte[] aes = null) => AddPak(new PakReader(stream, name, aes));

        public void AddPak(string file, IList<byte[]> aes, out int aesInd) => AddPak(new PakReader(file, aes, out aesInd));
        public void AddPak(Stream stream, string name, IList<byte[]> aes, out int aesInd) => AddPak(new PakReader(stream, name, aes, out aesInd));

        public void AddPak(PakReader reader)
        {
            for(int i = 0; i < reader.FileInfos.Length; i++)
            {
                (string Path, string Extension) = GetPath(reader.MountPoint + reader.FileNames[i]);
                if (!Index.ContainsKey(Path))
                {
                    var pak = Index[Path] = new PakPackage();
                    InsertEntry(reader.FileInfos[i], pak, Extension, reader);
                }
                else
                {
                    InsertEntry(reader.FileInfos[i], Index[Path], Extension, reader);
                }
            }
        }

        public PakPackage GetPackage(string name) => Index.TryGetValue(name.ToLowerInvariant(), out var ret) ? ret : null;

        #region IDictionary<string, PakPackage> Members

        public bool ContainsKey(string key) => Index.ContainsKey(key);

        ICollection<string> IDictionary<string, PakPackage>.Keys => Index.Keys;

        public bool TryGetValue(string key, out PakPackage value) => Index.TryGetValue(key, out value);

        ICollection<PakPackage> IDictionary<string, PakPackage>.Values => Index.Values;

        public PakPackage this[string key] => Index[key];

        void IDictionary<string, PakPackage>.Add(string key, PakPackage value) => throw new NotSupportedException();

        bool IDictionary<string, PakPackage>.Remove(string key) => throw new NotSupportedException();

        PakPackage IDictionary<string, PakPackage>.this[string key]
        {
            get => Index[key];
            set => throw new NotSupportedException();
        }

        #endregion

        #region ICollection<KeyValuePair<string, PakPackage>> Members

        public int Count => Index.Count;

        bool ICollection<KeyValuePair<string, PakPackage>>.Contains(KeyValuePair<string, PakPackage> item) => ((IDictionary)Index).Contains(item);

        void ICollection<KeyValuePair<string, PakPackage>>.CopyTo(KeyValuePair<string, PakPackage>[] array, int arrayIndex) => ((IDictionary)Index).CopyTo(array, arrayIndex);

        bool ICollection<KeyValuePair<string, PakPackage>>.IsReadOnly => true;

        void ICollection<KeyValuePair<string, PakPackage>>.Add(KeyValuePair<string, PakPackage> item) => throw new NotSupportedException();

        void ICollection<KeyValuePair<string, PakPackage>>.Clear() => throw new NotSupportedException();

        bool ICollection<KeyValuePair<string, PakPackage>>.Remove(KeyValuePair<string, PakPackage> item) => throw new NotSupportedException();

        #endregion

        #region IEnumerable<KeyValuePair<string, PakPackage>> Members

        public IEnumerator<KeyValuePair<string, PakPackage>> GetEnumerator() => Index.GetEnumerator();

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((IEnumerable)Index).GetEnumerator();

        #endregion

        #region IDictionary Members

        void IDictionary.Add(object key, object value) => throw new NotSupportedException();

        void IDictionary.Clear() => throw new NotSupportedException();

        bool IDictionary.Contains(object key) => ((IDictionary)Index).Contains(key);

        IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)Index).GetEnumerator();

        bool IDictionary.IsFixedSize => true;

        bool IDictionary.IsReadOnly => true;

        ICollection IDictionary.Keys => ((IDictionary)Index).Keys;

        void IDictionary.Remove(object key) => throw new NotSupportedException();

        ICollection IDictionary.Values => ((IDictionary)Index).Values;

        object IDictionary.this[object key]
        {
            get => ((IDictionary)Index)[key];
            set => throw new NotSupportedException();
        }

        void ICollection.CopyTo(Array array, int index) => ((IDictionary)Index).CopyTo(array, index);

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((IDictionary)Index).SyncRoot; // throws NotSupportedException, but more info

        #endregion

        #region IReadOnlyDictionary members

        IEnumerable<string> IReadOnlyDictionary<string, PakPackage>.Keys => Index.Keys;

        IEnumerable<PakPackage> IReadOnlyDictionary<string, PakPackage>.Values => Index.Values;

        #endregion IReadOnlyDictionary members
    }

    public sealed class PakPackage
    {
        public List<Extension> Extensions = new List<Extension>();

        public async Task<ExportObject[]> GetExportsAsync(bool ignoreErrors = true) => (await GetAssetReaderAsync(ignoreErrors).ConfigureAwait(false))?.Exports;

        AssetReader reader;
        public async Task<AssetReader> GetAssetReaderAsync(bool ignoreErrors = false) =>
            reader ?? (reader = Exportable ? new AssetReader(await GetPackageStreamAsync("uasset").ConfigureAwait(false), await GetPackageStreamAsync("uexp").ConfigureAwait(false), await GetPackageStreamAsync("ubulk").ConfigureAwait(false), ignoreErrors) : null);

        public bool Exportable => HasExtension("uasset") && HasExtension("uexp");

        public bool HasExtension(string extension) => Extensions.FindIndex(ext => ext.Ext == extension) != -1;

        public async Task<Stream> GetPackageStreamAsync(string extension)
        {
            var ext = Extensions.Find(e => e.Ext == extension);
            var ret = ext == null ? null : await ext.GetDataStreamAsync().ConfigureAwait(false);
            if (ret != null) ret.Position = 0;
            return ret;
        }
        

        public async Task<UObject> GetUObjectAsync() => (await GetExportsAsync().ConfigureAwait(false))[0] as UObject;

        SKImage image;
        public async Task<SKImage> GetTextureAsync() =>
            image ?? (image = (await GetExportsAsync().ConfigureAwait(false))[0] is Texture2D tex ? ImageExporter.GetImage(tex.textures[0].mips[0], tex.textures[0].pixel_format) : null);

        public sealed class Extension
        {
            public readonly string Ext;
            public readonly byte[] Key;
            public readonly FPakEntry Entry;
            public readonly Stream Stream;

            internal Extension(string extension, byte[] key, FPakEntry entry, Stream stream)
            {
                Ext = extension;
                Key = key;
                Entry = entry;
                Stream = stream;
            }

            MemoryStream dataStream;
            public async Task<MemoryStream> GetDataStreamAsync() =>
                dataStream ?? (dataStream = new MemoryStream(await FPakFile.GetDataAsync(Stream, Entry, Key).ConfigureAwait(false), false));
        }
    }
}
