using System;
using System.IO;
using System.Security.Cryptography;

namespace PakReader
{
    public sealed class SigFile
    {
        const uint Magic = 0x73832DAA;

        public readonly bool PAKHASH_USE_CRC;

        // Sig file version. Set to Legacy if the sig file is of an old version
        public readonly ESigVersion Version;

        // RSA encrypted hash
        public readonly byte[] EncryptedHash;

        // SHA1 hash of the chunk CRC data. Only valid after calling DecryptSignatureAndValidate
        public readonly byte[] DecryptedHash;

        public readonly uint[] CRCChunkHashes;
        public readonly byte[,] SHAChunkHashes;

        public SigFile(string path, bool useCRC) : this(File.OpenRead(path), useCRC) { }

        public SigFile(Stream stream, bool useCRC) : this(new BinaryReader(stream), useCRC) { }

        public SigFile(BinaryReader reader, bool useCRC)
        {
            PAKHASH_USE_CRC = useCRC;
            if (reader.ReadUInt32() != Magic)
            {
                throw new FileLoadException("Invalid file magic");
            }

            Version = (ESigVersion)reader.ReadInt32();
            EncryptedHash = reader.ReadTArray(() => reader.ReadByte());
            if (PAKHASH_USE_CRC)
            {
                CRCChunkHashes = reader.ReadTArray(() => reader.ReadUInt32());
            }
            else
            {
                var len = reader.ReadInt32();
                SHAChunkHashes = new byte[len, 20];
                var buf = new byte[len * 20];
                reader.BaseStream.Read(buf, 0, len * 20);
                Buffer.BlockCopy(buf, 0, SHAChunkHashes, 0, len * 20);
            }
        }

        /*bool DecryptSignatureAndValidate(byte[] InKey, string InFilename)
	    {
		    if (Version == ESigVersion.Invalid)
		    {
			    UE_LOG(LogPakFile, Warning, TEXT("Pak signature file for '%s' was invalid"), *InFilename);
		    }
		    else
		    {
			    byte[] Decrypted;
                System.Security.Cryptography.RSA.Create().  ()
                int32 BytesDecrypted = FRSA::DecryptPublic(EncryptedHash, Decrypted, InKey);
			    if (BytesDecrypted == ARRAY_COUNT(FSHAHash::Hash))
			    {
				    FSHAHash CurrentHash = ComputeCurrentMasterHash();
				    if (FMemory::Memcmp(Decrypted.GetData(), CurrentHash.Hash, Decrypted.Num()) == 0)
				    {
					    return true;
				    }
				    else
				    {
					    UE_LOG(LogPakFile, Warning, TEXT("Pak signature table validation failed for '%s'! Expected %s, Received %s"), * InFilename, * DecryptedHash.ToString(), * CurrentHash.ToString());
				    }
			    }
			    else
			    {
				    UE_LOG(LogPakFile, Warning, TEXT("Pak signature table validation failed for '%s'! Failed to decrypt signature"), * InFilename);
			    }
		    }

		    FPakPlatformFile::GetPakMasterSignatureTableCheckFailureHandler().Broadcast(InFilename);
		    return false;
	    }*/

        /*unsafe byte[] ComputeCurrentMasterHash()
        {
            using (var sha = SHA1.Create())
            {
                //var buf = new byte[(PAKHASH_USE_CRC ? sizeof(uint) : 20) * ChunkHashes.Length];
                if (PAKHASH_USE_CRC)
                {
                    var arr = (uint[])(object)CRCChunkHashes;
                    fixed(uint* p = &arr[0])
                    {
                        var p2 = (byte*)p;
                        var arr = (byte[])p2;
                        var n = (byte)2;
                        var p3 = (byte[])&n;
                    }
                }
            }
        }*/
    }

    public enum ESigVersion
    {
        Invalid,
        First,

        Last,
        Latest = Last - 1
    };
}
