using System.IO;
using System.Security.Cryptography;

namespace nfs2iso2nfs.Helpers
{
    public class KeyHelper
    {
        /// <summary>
        /// Asynchronously retrieves a key from a given file directory.
        /// </summary>
        /// <param name="keyDir">Path to the key file.</param>
        /// <returns>A byte array representing the key or null if the key size is not 16 bytes.</returns>
        public static async Task<byte[]?> GetKeyAsync(string keyDir)
        {
            using var keyFile = new BinaryReader(File.OpenRead(keyDir));
            var keySize = keyFile.BaseStream.Length;

            if (keySize != 16)
                return null;

            var key = new byte[0x10];
            await keyFile.BaseStream.ReadAsync(key).ConfigureAwait(false);

            return key;
        }

        /// <summary>
        /// Creates an Aes instance with CBC mode and no padding.
        /// </summary>
        /// <param name="key">Encryption key.</param>
        /// <param name="iv">Initialization Vector.</param>
        /// <returns>An Aes instance configured with the provided key and IV.</returns>
        public static Aes CreateAes128Cbc(byte[] key, byte[] iv)
        {
            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = key;
            aes.IV = iv;

            return aes;
        }

        /// <summary>
        /// Encrypts or decrypts data using the provided Aes instance.
        /// </summary>
        /// <param name="aes">The Aes instance to use.</param>
        /// <param name="data">Data to encrypt or decrypt.</param>
        /// <param name="encrypt">Whether to encrypt (true) or decrypt (false). Default is false.</param>
        /// <returns>Encrypted or decrypted data as a byte array.</returns>
        public static byte[] CryptAes128Cbc(Aes aes, byte[] data, bool encrypt = false)
        {
            using (ICryptoTransform itc = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor())
                return itc.TransformFinalBlock(data, 0, data.Length);
        }
    }
}
