using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PakReader
{
    static class AESDecryptor
    {
        const int BLOCK_SIZE = 16 * 8;
        static readonly Rijndael Cipher;

        static AESDecryptor()
        {
            Cipher = Rijndael.Create();
            Cipher.Mode = CipherMode.ECB;
            Cipher.Padding = PaddingMode.Zeros;
            Cipher.BlockSize = BLOCK_SIZE;
        }

        static ICryptoTransform GetDecryptor(byte[] key) => Cipher.CreateDecryptor(key, null);

        public static int FindKey(byte[] data, IList<byte[]> keys)
        {
            byte[] block = new byte[BLOCK_SIZE];
            for (int i = 0; i < keys.Count; i++)
            {
                using (var crypto = GetDecryptor(keys[i]))
                    crypto.TransformBlock(data, 0, BLOCK_SIZE, block, 0);
                int stringLen = BitConverter.ToInt32(block, 0);
                if (stringLen > 512 || stringLen < -512)
                    continue;
                if (stringLen < 0)
                {
                    if (BitConverter.ToUInt16(block, (stringLen - 1) * 2 + 4) != 0)
                        continue;
                }
                else
                {
                    if (block[stringLen - 1 + 4] != 0)
                        continue;
                }
                return i;
            }
            return -1;
        }

        public static byte[] DecryptAES(byte[] data, byte[] key)
        {
            using (var crypto = GetDecryptor(key))
                return crypto.TransformFinalBlock(data, 0, data.Length);
        }
    }
}
