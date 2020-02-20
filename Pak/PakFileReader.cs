using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PakReader.Parsers.Objects;

namespace PakReader.Pak
{
    public sealed class PakFileReader : IReadOnlyDictionary<string, FPakEntry>
    {
        public FPakInfo Info { get; }
        public Stream Stream { get; }
        public bool CaseSensitive { get; }
        public byte[] Key { get; private set; }
        public string MountPoint { get; private set; }
        public bool Initialized { get; private set; }

        readonly BinaryReader Reader;
        Dictionary<string, FPakEntry> Entries;

        // Buffered streams increase performance dramatically
        public PakFileReader(string file, bool caseSensitive = true) : this(new BufferedStream(File.OpenRead(file)), caseSensitive) { }

        public PakFileReader(Stream stream, bool caseSensitive = true)
        {
            Stream = stream;
            CaseSensitive = caseSensitive;
            Reader = new BinaryReader(stream, Encoding.Default, true);
            stream.Seek(-FPakInfo.SERIALIZED_SIZE, SeekOrigin.End);
            Info = new FPakInfo(Reader);
        }

        public bool TryReadIndex(byte[] key, PakFilter filter = null)
        {
            ReadIndexInternal(key, filter, out var exc);
            return exc == null;
        }

        public void ReadIndex(byte[] key, PakFilter filter = null)
        {
            ReadIndexInternal(key, filter, out var exc);
            if (exc != null)
                throw exc;
        }

        void ReadIndexInternal(byte[] key, PakFilter filter, out Exception exc)
        {
            if (Initialized)
            {
                exc = new InvalidOperationException("Index is already initialized");
                return;
            }

            if (Info.bEncryptedIndex && key == null)
            {
                exc = new ArgumentException("Index is encrypted but no key was provided", nameof(key));
                return;
            }

            Stream.Position = Info.IndexOffset;

            BinaryReader IndexReader;
            if (Info.bEncryptedIndex)
            {
                IndexReader = new BinaryReader(new MemoryStream(AESDecryptor.DecryptAES(Reader.ReadBytes((int)Info.IndexSize), key)));
                int stringLen = IndexReader.ReadInt32();
                if (stringLen > 512 || stringLen < -512)
                {
                    exc = new ArgumentException("The provided key is invalid", nameof(key));
                    return;
                }
                if (stringLen < 0)
                {
                    IndexReader.BaseStream.Position += (stringLen - 1) * 2;
                    if (IndexReader.ReadUInt16() != 0)
                    {
                        exc = new ArgumentException("The provided key is invalid", nameof(key));
                        return;
                    }
                }
                else
                {
                    IndexReader.BaseStream.Position += stringLen - 1;
                    if (IndexReader.ReadByte() != 0)
                    {
                        exc = new ArgumentException("The provided key is invalid", nameof(key));
                        return;
                    }
                }
                IndexReader.BaseStream.Position = 0;
            }
            else
            {
                IndexReader = Reader;
            }

            // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/PakFile/Private/IPlatformFilePak.cpp#L4509

            MountPoint = IndexReader.ReadFString();
            if (MountPoint.StartsWith("../../.."))
            {
                MountPoint = MountPoint.Substring(8);
            }
            else
            {
                // Weird mount point location...
                MountPoint = "/";
            }
            if (!CaseSensitive)
            {
                MountPoint = MountPoint.ToLowerInvariant();
            }

            var NumEntries = IndexReader.ReadInt32();
            Entries = new Dictionary<string, FPakEntry>(NumEntries);
            for (int i = 0; i < NumEntries; i++)
            {
                var filename = CaseSensitive ? IndexReader.ReadFString() : IndexReader.ReadFString().ToLowerInvariant();
                var entry = new FPakEntry(IndexReader, Info.Version);
                // if there is no filter OR the filter passes
                if (filter == null || filter.CheckFilter(MountPoint + filename, CaseSensitive))
                {
                    // Filename is without the MountPoint concatenated to save memory
                    Entries[filename] = entry;
                }
            }

            if (Info.bEncryptedIndex)
            {
                // underlying stream is a MemoryStream of the decrypted index, might improve performance with a crypto stream of some sort
                IndexReader.Dispose();
            }
            Reader.Dispose();
            Key = key;
            Initialized = true;
            exc = null;
        }

        // path is without the mountpoint (not even if it's "/")
        public bool TryGetFile(string path, out ArraySegment<byte> ret)
        {
            if (Entries.TryGetValue(CaseSensitive ? path : path.ToLowerInvariant(), out var entry))
            {
                ret = entry.GetData(Stream, Key);
                return true;
            }
            ret = null;
            return false;
        }
        public ReadOnlyMemory<byte> GetFile(string path) => Entries[CaseSensitive ? path : path.ToLowerInvariant()].GetData(Stream, Key);

        // IReadOnlyDictionary implementation (to prevent writing to the Entries dictionary

        // TODO: Make these methods respect CaseSensitive property
        FPakEntry IReadOnlyDictionary<string, FPakEntry>.this[string key] => Entries[key];
        IEnumerable<string> IReadOnlyDictionary<string, FPakEntry>.Keys => Entries.Keys;
        IEnumerable<FPakEntry> IReadOnlyDictionary<string, FPakEntry>.Values => Entries.Values;
        int IReadOnlyCollection<KeyValuePair<string, FPakEntry>>.Count => Entries.Count;

        bool IReadOnlyDictionary<string, FPakEntry>.ContainsKey(string key) => Entries.ContainsKey(key);
        IEnumerator<KeyValuePair<string, FPakEntry>> IEnumerable<KeyValuePair<string, FPakEntry>>.GetEnumerator() => Entries.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();
        bool IReadOnlyDictionary<string, FPakEntry>.TryGetValue(string key, out FPakEntry value) => Entries.TryGetValue(key, out value);
    }
}
