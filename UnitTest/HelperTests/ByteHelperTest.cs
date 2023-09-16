using nfs2iso2nfs.Helpers;

namespace UnitTest.HelperTests
{
    [TestClass]
    public class ByteHelperTest
    {
        #region GetHeader
        [TestMethod]
        public void GetHeaderNotNull()
        {
            var location = Path.Combine(Environment.CurrentDirectory, "GetHeaderEmptyFile.txt");
            File.Create(location).Dispose();

            Assert.IsNotNull(ByteHelper.GetHeaderAsync(location).Result);
            File.Delete(location);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetHeaderNoPath()
        {
            await ByteHelper.GetHeaderAsync("");
        }

        #endregion GetHeader
        #region BuildZero
        [TestMethod]
        public void BuildZeroNotNull()
        {
            Assert.IsNotNull(ByteHelper.BuildZero(0));
        }
        [TestMethod]
        public void BuildZeroNotEqual()
        {
            var size = 8;
            var byteArray = new byte[size];
            Assert.AreNotEqual(byteArray, ByteHelper.BuildZero(size));
        }
        [TestMethod]
        public void BuildZeroEqual()
        {
            var size = 8;
            var array1 = ByteHelper.BuildZero(size);
            var array2 = ByteHelper.BuildZero(size);

            var areEqual = true;
            for (var i = 0; i < size; i++)
                if (array1[i] != array2[i])
                    areEqual = false;

            Assert.IsTrue(areEqual);
        }
        #endregion BuildZero
        #region Sort
        [TestMethod]
        public void Sort1D()
        {
            var array = new int[10];
            var sortedArray = new int[10];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = i;
                sortedArray[i] = i;
            }

            var areEqual = true;
            sortedArray = ByteHelper.Sort(sortedArray, array.Length);
            for (var i = 0; i < sortedArray.Length; i++)
                if (sortedArray[i] != array[i])
                    areEqual = false;

            Assert.IsFalse(areEqual);
        }
        [TestMethod]
        public void Sort()
        {
            var array = new int[10,10];
            var sortedArray = new int[10,10];
            for (var i = 0; i < 10; i++)
                for(var j = 0; j < 10; j++)
                {
                    array[i,j] = i+j;
                    sortedArray[i,j] = i+j;
                }

            var areEqual = true;
            sortedArray = ByteHelper.Sort(sortedArray, 10);
            for (var i = 0; i < 10; i++)
                for(var j = 0; j < 10; j++)
                if (sortedArray[i,j] != array[i,j])
                    areEqual = false;

            Assert.IsFalse(areEqual);
        }
        #endregion Sort
        #region ByteArrayCompare
        [TestMethod]
        public void ByteArrayCompare()
        {
            var size = 10;
            var array1 = new byte[size];
            var array2 = new byte[size];
            
            for (var i = 0; i < size; i++)
            {
                array1[i] = (byte)i;
                array2[i] = (byte)i;
            }

            Assert.IsTrue(ByteHelper.ByteArrayCompare(array1, array2));
        }
        [TestMethod]
        public void ByteArrayCompareNotSame()
        {
            var size = 10;
            var array1 = new byte[size];
            var array2 = new byte[size];

            for (var i = 0; i < size; i++)
            {
                array1[i] = (byte)(i - size);
                array2[i] = (byte)i;
            }

            Assert.IsFalse(ByteHelper.ByteArrayCompare(array1, array2));
        }
        [TestMethod]
        public void ByteArrayCompareOneArrayNoValues()
        {
            var size = 10;
            var array1 = new byte[size];
            var array2 = new byte[size];

            for (var i = 0; i < size; i++)
                array2[i] = (byte)i;

            Assert.IsFalse(ByteHelper.ByteArrayCompare(array1, array2));
        }
        [TestMethod]
        public void ByteArrayCompareBothArrayNoValue()
        {
            var size = 10;
            var array1 = new byte[size];
            var array2 = new byte[size];

            Assert.IsTrue(ByteHelper.ByteArrayCompare(array1, array2));
        }
        #endregion ByteArrayCompare
    }
}