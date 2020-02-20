using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            if (Info.Version >= EPakVersion.PATH_HASH_INDEX)
            {
                ReadIndexUpdated(IndexReader, key, Stream.Length, filter);
            }
            else
            {

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

        void ReadIndexUpdated(BinaryReader reader, byte[] aesKey, long totalSize, PakFilter filter)
        {
            MountPoint = reader.ReadFString();
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
            var NumEntries = reader.ReadInt32();
            var PathHashSeed = reader.ReadUInt64();

            bool bReaderHasPathHashIndex = false;
            long PathHashIndexOffset = -1; // INDEX_NONE
            long PathHashIndexSize = 0;
            FSHAHash PathHashIndexHash = default;
            bReaderHasPathHashIndex = reader.ReadInt32() != 0;
            if (bReaderHasPathHashIndex)
            {
                PathHashIndexOffset = reader.ReadInt64();
                PathHashIndexSize = reader.ReadInt64();
                PathHashIndexHash = new FSHAHash(reader);
                bReaderHasPathHashIndex = bReaderHasPathHashIndex && PathHashIndexOffset != -1;
            }

            bool bReaderHasFullDirectoryIndex = false;
            long FullDirectoryIndexOffset = -1; // INDEX_NONE
            long FullDirectoryIndexSize = 0;
            FSHAHash FullDirectoryIndexHash = default;
            bReaderHasFullDirectoryIndex = reader.ReadInt32() != 0;
            if (bReaderHasFullDirectoryIndex)
            {
                FullDirectoryIndexOffset = reader.ReadInt64();
                FullDirectoryIndexSize = reader.ReadInt64();
                FullDirectoryIndexHash = new FSHAHash(reader);
                bReaderHasFullDirectoryIndex = bReaderHasFullDirectoryIndex && FullDirectoryIndexOffset != -1;
            }

            byte[] EncodedPakEntries = reader.ReadTArray(() => reader.ReadByte());
            File.WriteAllBytes("pakentryencoded", EncodedPakEntries);

            int FilesNum = reader.ReadInt32();
            if (FilesNum < 0)
            {
                // Should not be possible for any values in the PrimaryIndex to be invalid, since we verified the index hash
                throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
            }
            FPakEntry[] Files = new FPakEntry[FilesNum]; // from what i can see, there aren't any???
            if (FilesNum > 0)
            {
                for (int FileIndex = 0; FileIndex < FilesNum; ++FileIndex)
                {
                    Files[FileIndex] = new FPakEntry(reader, Info.Version);
                }
            }

            // Decide which SecondaryIndex(es) to load
            bool bWillUseFullDirectoryIndex;
            bool bWillUsePathHashIndex;
            bool bReadFullDirectoryIndex;
            if (bReaderHasPathHashIndex && bReaderHasFullDirectoryIndex)
            {
                bWillUseFullDirectoryIndex = false; // https://github.com/EpicGames/UnrealEngine/blob/79a64829237ae339118bb50b61d84e4599c14e8a/Engine/Source/Runtime/PakFile/Private/IPlatformFilePak.cpp#L5628
                bWillUsePathHashIndex = !bWillUseFullDirectoryIndex;
                bool bWantToReadFullDirectoryIndex = false;
                bReadFullDirectoryIndex = bReaderHasFullDirectoryIndex && bWantToReadFullDirectoryIndex;
            }
            else if (bReaderHasPathHashIndex)
            {
                bWillUsePathHashIndex = true;
                bWillUseFullDirectoryIndex = false;
                bReadFullDirectoryIndex = false;
            }
            else if (bReaderHasFullDirectoryIndex)
            {
                // We don't support creating the PathHash Index at runtime; we want to move to having only the PathHashIndex, so supporting not having it at all is not useful enough to write
                bWillUsePathHashIndex = false;
                bWillUseFullDirectoryIndex = true;
                bReadFullDirectoryIndex = true;
            }
            else
            {
                // It should not be possible for PrimaryIndexes to be built without a PathHashIndex AND without a FullDirectoryIndex; CreatePakFile in UnrealPak.exe has a check statement for it.
                throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
            }

            // Load the Secondary Index(es)
            byte[] PathHashIndexData;
            Dictionary<ulong, int> PathHashIndex;
            BinaryReader PathHashIndexReader = default;
            bool bHasPathHashIndex;
            if (bWillUsePathHashIndex)
            {
                if (PathHashIndexOffset < 0 || totalSize < (PathHashIndexOffset + PathHashIndexSize))
                {
                    // Should not be possible for these values (which came from the PrimaryIndex) to be invalid, since we verified the index hash of the PrimaryIndex
                    throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
                    //UE_LOG(LogPakFile, Log, TEXT(" Filename: %s"), *PakFilename);
                    //UE_LOG(LogPakFile, Log, TEXT(" Total Size: %d"), Reader->TotalSize());
                    //UE_LOG(LogPakFile, Log, TEXT(" PathHashIndexOffset : %d"), PathHashIndexOffset);
                    //UE_LOG(LogPakFile, Log, TEXT(" PathHashIndexSize: %d"), PathHashIndexSize);
                }
                Reader.BaseStream.Position = PathHashIndexOffset;
                PathHashIndexData = Reader.ReadBytes((int)PathHashIndexSize);
                File.WriteAllBytes("indexdata.daa", PathHashIndexData);

                {
                    if (!DecryptAndValidateIndex(Reader, ref PathHashIndexData, aesKey, PathHashIndexHash, out var ComputedHash))
                    {
                        throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
                        //UE_LOG(LogPakFile, Log, TEXT(" Filename: %s"), *PakFilename);
                        //UE_LOG(LogPakFile, Log, TEXT(" Encrypted: %d"), Info.bEncryptedIndex);
                        //UE_LOG(LogPakFile, Log, TEXT(" Total Size: %d"), Reader->TotalSize());
                        //UE_LOG(LogPakFile, Log, TEXT(" Index Offset: %d"), FullDirectoryIndexOffset);
                        //UE_LOG(LogPakFile, Log, TEXT(" Index Size: %d"), FullDirectoryIndexSize);
                        //UE_LOG(LogPakFile, Log, TEXT(" Stored Index Hash: %s"), *PathHashIndexHash.ToString());
                        //UE_LOG(LogPakFile, Log, TEXT(" Computed Index Hash: %s"), *ComputedHash.ToString());
                    }
                }

                PathHashIndexReader = new BinaryReader(new MemoryStream(PathHashIndexData));
                PathHashIndex = ReadPathHashIndex(PathHashIndexReader);
                bHasPathHashIndex = true;
            }

            var DirectoryIndex = new Dictionary<string, Dictionary<string, int>>();
            bool bHasFullDirectoryIndex;
            if (!bReadFullDirectoryIndex)
            {
                DirectoryIndex = ReadDirectoryIndex(PathHashIndexReader);
                bHasFullDirectoryIndex = false;
            }
            if (DirectoryIndex.Count == 0)
            {
                if (totalSize < (FullDirectoryIndexOffset + FullDirectoryIndexSize) ||
                    FullDirectoryIndexOffset < 0)
                {
                    // Should not be possible for these values (which came from the PrimaryIndex) to be invalid, since we verified the index hash of the PrimaryIndex
                    throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
                    //UE_LOG(LogPakFile, Log, TEXT(" Filename: %s"), *PakFilename);
                    //UE_LOG(LogPakFile, Log, TEXT(" Total Size: %d"), Reader->TotalSize());
                    //UE_LOG(LogPakFile, Log, TEXT(" FullDirectoryIndexOffset : %d"), FullDirectoryIndexOffset);
                    //UE_LOG(LogPakFile, Log, TEXT(" FullDirectoryIndexSize: %d"), FullDirectoryIndexSize);
                }
                Reader.BaseStream.Position = FullDirectoryIndexOffset;
                byte[] FullDirectoryIndexData = Reader.ReadBytes((int)FullDirectoryIndexSize);

                {
                    if (!DecryptAndValidateIndex(Reader, ref FullDirectoryIndexData, aesKey, FullDirectoryIndexHash, out var ComputedHash))
                    {
                        throw new FileLoadException("Corrupt pak PrimaryIndex detected!");
                        //UE_LOG(LogPakFile, Log, TEXT(" Filename: %s"), *PakFilename);
                        //UE_LOG(LogPakFile, Log, TEXT(" Encrypted: %d"), Info.bEncryptedIndex);
                        //UE_LOG(LogPakFile, Log, TEXT(" Total Size: %d"), Reader->TotalSize());
                        //UE_LOG(LogPakFile, Log, TEXT(" Index Offset: %d"), FullDirectoryIndexOffset);
                        //UE_LOG(LogPakFile, Log, TEXT(" Index Size: %d"), FullDirectoryIndexSize);
                        //UE_LOG(LogPakFile, Log, TEXT(" Stored Index Hash: %s"), *FullDirectoryIndexHash.ToString());
                        //UE_LOG(LogPakFile, Log, TEXT(" Computed Index Hash: %s"), *ComputedHash.ToString());
                    }
                }

                var SecondaryIndexReader = new BinaryReader(new MemoryStream(FullDirectoryIndexData));
                DirectoryIndex = ReadDirectoryIndex(SecondaryIndexReader);
                bHasFullDirectoryIndex = true;
            }

            Entries = new Dictionary<string, FPakEntry>(NumEntries);
            foreach (var (dirname, dir) in DirectoryIndex)
            {
                foreach(var (filename, pakLocation) in dir)
                {
                    var path = dirname + filename;
                    if (!CaseSensitive)
                    {
                        path = path.ToLowerInvariant();
                    }
                    // if there is no filter OR the filter passes
                    if (filter == null || filter.CheckFilter(MountPoint + filename, CaseSensitive))
                    {
                        // Filename is without the MountPoint concatenated to save memory
                        Entries[path] = GetEntry(pakLocation, EncodedPakEntries);
                    }
                }
            }
        }

        Dictionary<ulong, int> ReadPathHashIndex(BinaryReader reader)
        {
            var ret = new Dictionary<ulong, int>();
            var keys = reader.ReadTArray(() => (reader.ReadUInt64(), reader.ReadInt32()));
            foreach (var (k, v) in keys)
            {
                ret[k] = v;
            }
            return ret;
        }

        Dictionary<string, Dictionary<string, int>> ReadDirectoryIndex(BinaryReader reader)
        {
            var ret = new Dictionary<string, Dictionary<string, int>>();
            var keys = reader.ReadTArray(() => (reader.ReadFString(), ReadFPakDirectory(reader)));
            foreach(var (k,v) in keys)
            {
                ret[k] = v;
            }
            return ret;
        }

        Dictionary<string, int> ReadFPakDirectory(BinaryReader reader)
        {
            var ret = new Dictionary<string, int>();
            var keys = reader.ReadTArray(() => (reader.ReadFString(), reader.ReadInt32()));
            foreach (var (k, v) in keys)
            {
                ret[k] = v;
            }
            return ret;
        }

        bool DecryptAndValidateIndex(BinaryReader reader, ref byte[] IndexData, byte[] aesKey, FSHAHash ExpectedHash, out FSHAHash OutHash)
        {
            if (Info.bEncryptedIndex)
            {
                IndexData = AESDecryptor.DecryptAES(IndexData, aesKey);
            }
            OutHash = ExpectedHash; // too lazy to actually check against the hash
            // https://github.com/EpicGames/UnrealEngine/blob/79a64829237ae339118bb50b61d84e4599c14e8a/Engine/Source/Runtime/PakFile/Private/IPlatformFilePak.cpp#L5371
            return true;
        }

        FPakEntry GetEntry(int pakLocation, byte[] encodedPakEntries)
        {
            if (pakLocation >= 0)
            {
                // Grab the big bitfield value:
                // Bit 31 = Offset 32-bit safe?
                // Bit 30 = Uncompressed size 32-bit safe?
                // Bit 29 = Size 32-bit safe?
                // Bits 28-23 = Compression method
                // Bit 22 = Encrypted
                // Bits 21-6 = Compression blocks count
                // Bits 5-0 = Compression block size

                // Filter out the CompressionMethod.

                long Offset, UncompressedSize, Size;
                uint CompressionMethodIndex, CompressionBlockSize;
                bool Encrypted, Deleted;

                uint Value = BitConverter.ToUInt32(encodedPakEntries, pakLocation);
                pakLocation += sizeof(uint);

                CompressionMethodIndex = ((Value >> 23) & 0x3f);

                // Test for 32-bit safe values. Grab it, or memcpy the 64-bit value
                // to avoid alignment exceptions on platforms requiring 64-bit alignment
                // for 64-bit variables.
                //
                // Read the Offset.
                bool bIsOffset32BitSafe = (Value & (1 << 31)) != 0;
                if (bIsOffset32BitSafe)
                {
                    Offset = BitConverter.ToUInt32(encodedPakEntries, pakLocation);
                    pakLocation += sizeof(uint);
                }
                else
                {
                    Offset = BitConverter.ToInt64(encodedPakEntries, pakLocation);
                    pakLocation += sizeof(long);
                }

                // Read the UncompressedSize.
                bool bIsUncompressedSize32BitSafe = (Value & (1 << 30)) != 0;
                if (bIsUncompressedSize32BitSafe)
                {
                    UncompressedSize = BitConverter.ToUInt32(encodedPakEntries, pakLocation);
                    pakLocation += sizeof(uint);
                }
                else
                {
                    UncompressedSize = BitConverter.ToInt64(encodedPakEntries, pakLocation);
                    pakLocation += sizeof(long);
                }

                // Fill in the Size.
                if (CompressionMethodIndex != 0)
                {
                    // Size is only present if compression is applied.
                    bool bIsSize32BitSafe = (Value & (1 << 29)) != 0;
                    if (bIsSize32BitSafe)
                    {
                        Size = BitConverter.ToUInt32(encodedPakEntries, pakLocation);
                        pakLocation += sizeof(uint);
                    }
                    else
                    {
                        Size = BitConverter.ToInt64(encodedPakEntries, pakLocation);
                        pakLocation += sizeof(long);
                    }
                }
                else
                {
                    // The Size is the same thing as the UncompressedSize when
                    // CompressionMethod == COMPRESS_None.
                    Size = UncompressedSize;
                }

                // Filter the encrypted flag.
                Encrypted = (Value & (1 << 22)) != 0;

                // This should clear out any excess CompressionBlocks that may be valid in the user's
                // passed in entry.
                var CompressionBlocksCount = (Value >> 6) & 0xffff;
                FPakCompressedBlock[] CompressionBlocks = new FPakCompressedBlock[CompressionBlocksCount];

                // Filter the compression block size or use the UncompressedSize if less that 64k.
                CompressionBlockSize = 0;
                if (CompressionBlocksCount > 0)
                {
                    CompressionBlockSize = UncompressedSize < 65536 ? (uint)UncompressedSize : ((Value & 0x3f) << 11);
                }

                // Set bDeleteRecord to false, because it obviously isn't deleted if we are here.
                Deleted = false;

                // Base offset to the compressed data
                long BaseOffset = true ? 0 : Offset; // HasRelativeCompressedChunkOffsets -> Version >= PakFile_Version_RelativeChunkOffsets

                // Handle building of the CompressionBlocks array.
                if (CompressionBlocks.Length == 1 && !Encrypted)
                {
                    // If the number of CompressionBlocks is 1, we didn't store any extra information.
                    // Derive what we can from the entry's file offset and size.
                    var start = BaseOffset + FPakEntry.GetSize(EPakVersion.LATEST, CompressionMethodIndex, CompressionBlocksCount);
                    CompressionBlocks[0] = new FPakCompressedBlock(start, start + Size);
                }
                else if (CompressionBlocks.Length > 0)
                {
                    // Get the right pointer to start copying the CompressionBlocks information from.

                    // Alignment of the compressed blocks
                    var CompressedBlockAlignment = Encrypted ? AESDecryptor.BLOCK_SIZE : 1;

                    // CompressedBlockOffset is the starting offset. Everything else can be derived from there.
                    long CompressedBlockOffset = BaseOffset + FPakEntry.GetSize(EPakVersion.LATEST, CompressionMethodIndex, CompressionBlocksCount);
                    for (int CompressionBlockIndex = 0; CompressionBlockIndex < CompressionBlocks.Length; ++CompressionBlockIndex)
                    {
                        CompressionBlocks[CompressionBlockIndex] = new FPakCompressedBlock(CompressedBlockOffset, CompressedBlockOffset + BitConverter.ToUInt32(encodedPakEntries, pakLocation));
                        pakLocation += sizeof(uint);
                        {
                            var toAlign = CompressionBlocks[CompressionBlockIndex].CompressedEnd - CompressionBlocks[CompressionBlockIndex].CompressedStart;
                            CompressedBlockOffset += toAlign + CompressedBlockAlignment - (toAlign % CompressedBlockAlignment);
                        }
                    }
                }
                return new FPakEntry(Offset, Size, UncompressedSize, new byte[20], CompressionBlocks, CompressionBlockSize, CompressionMethodIndex, (byte)((Encrypted ? 0x01 : 0x00) | (Deleted ? 0x02 : 0x00)));
            }
            else
            {
                pakLocation = -(pakLocation + 1);
                throw new FileLoadException("list indexes aren't supported");
            }
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
