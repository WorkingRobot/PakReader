using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PakReader.Parsers.Objects
{
    public readonly struct FPakInfo
    {
        const uint PAK_FILE_MAGIC = 0x5A6F12E1;
        const int COMPRESSION_METHOD_NAME_LEN = 32;
        const int MAX_NUM_COMPRESSION_METHODS = 5;


        // Magic                                        //   4 bytes
        public readonly EPakVersion Version;            //   4 bytes
        public readonly long IndexOffset;               //   8 bytes
        public readonly long IndexSize;                 //   8 bytes
        public readonly FSHAHash IndexHash;             //  20 bytes
        public readonly bool bEncryptedIndex;           //   1 byte
        public readonly FGuid EncryptionKeyGuid;        //  16 bytes
        public readonly string[] CompressionMethods;    // 160 bytes
                                                        // 221 bytes total
        
        // I calculate the size myself instead of asking for an input version
        // https://github.com/EpicGames/UnrealEngine/blob/8b6414ae4bca5f93b878afadcc41ab518b09984f/Engine/Source/Runtime/PakFile/Public/IPlatformFilePak.h#L138
        internal const int SERIALIZED_SIZE = 221;

        internal FPakInfo(BinaryReader reader)
        {
            // Serialize if version is at least EPakVersion.ENCRYPTION_KEY_GUID
            EncryptionKeyGuid = new FGuid(reader);
            bEncryptedIndex = reader.ReadByte() != 0;
            
            if (reader.ReadUInt32() != PAK_FILE_MAGIC)
            {
                // UE4 tries to handle old versions but I'd rather not deal with that right now
                throw new FileLoadException("Invalid pak magic");
            }

            Version = (EPakVersion)reader.ReadInt32();
            IndexOffset = reader.ReadInt64();
            IndexSize = reader.ReadInt64();
            IndexHash = new FSHAHash(reader);

            // I'd do some version checking here, but I'd rather not care to check that you loaded a pak file from 2003
            // https://github.com/EpicGames/UnrealEngine/blob/8b6414ae4bca5f93b878afadcc41ab518b09984f/Engine/Source/Runtime/PakFile/Public/IPlatformFilePak.h#L185

            if (Version < EPakVersion.FNAME_BASED_COMPRESSION_METHOD)
            {
                CompressionMethods = new string[] { "Zlib", "Gzip", "Oodle" };
            }
            else
            {
                int BufferSize = COMPRESSION_METHOD_NAME_LEN * MAX_NUM_COMPRESSION_METHODS;
                byte[] Methods = reader.ReadBytes(BufferSize);
                var MethodList = new List<string>(MAX_NUM_COMPRESSION_METHODS);
                for (int i = 0; i < MAX_NUM_COMPRESSION_METHODS; i++)
                {
                    if (Methods[i*COMPRESSION_METHOD_NAME_LEN] != 0)
                    {
                        MethodList.Add(Encoding.ASCII.GetString(Methods, i * COMPRESSION_METHOD_NAME_LEN, COMPRESSION_METHOD_NAME_LEN));
                    }
                }
                CompressionMethods = MethodList.ToArray();
            }
        }
    }
}
