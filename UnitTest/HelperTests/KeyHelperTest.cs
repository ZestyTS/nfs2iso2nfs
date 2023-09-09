using nfs2iso2nfs.Helpers;
using System.Security.Cryptography;

namespace UnitTest.HelperTests
{
    [TestClass]
    public class KeyHelperTest
    {
        #region GetKey
        [TestMethod]
        public void GetKey()
        {
            var location = Path.Combine(Environment.CurrentDirectory, "GetKey.txt");
            File.Create(location).Dispose();

            Assert.IsNull(KeyHelper.GetKeyAsync(location).Result);
            File.Delete(location);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetKeyNoPath()
        {
            await KeyHelper.GetKeyAsync("");
        }

        #endregion GetKey
        #region CreateAes128Cbc
        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void CreateAes128CbcEmptyParams()
        {
            KeyHelper.CreateAes128Cbc(Array.Empty<byte>(), Array.Empty<byte>());
        }
        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void CreateAes128CbcFakeBytes()
        {
            var byte1 = new byte[1] { 1 };
            var byte2 = new byte[1] { 1 };

            KeyHelper.CreateAes128Cbc(byte1, byte2);
        }
        #endregion CreateAes128Cbc
        #region CryptAes128Cbc
        [TestMethod]
        public void EncryptAes128Cbc()
        {
            Assert.Inconclusive("Not sure how to properly make an Aes");
        }
        [TestMethod]
        public void DecryptAes128Cbc()
        {
            Assert.Inconclusive("Not sure how to properly make an Aes");
        }
        #endregion CryptAes128Cbc

    }
}