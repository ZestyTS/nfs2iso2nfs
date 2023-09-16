using System;

namespace nfs2iso2nfs.Helpers
{
    public static class ByteHelper
    {
        /// <summary>
        /// Reads the header of a given binary file asynchronously.
        /// </summary>
        /// <param name="inFile">Path of the input file.</param>
        /// <returns>A byte array representing the header of the file.</returns>
        public static async Task<byte[]> GetHeaderAsync(string inFile)
        {
            // Open the file for reading
            using var file = new BinaryReader(File.OpenRead(inFile));

            // Allocate a buffer for the header
            var buffer = new byte[0x200];

            // Read the header data into the buffer
            await file.BaseStream.ReadAsync(buffer).ConfigureAwait(false);

            return buffer;
        }

        /// <summary>
        /// Builds a byte array filled with zeros.
        /// </summary>
        /// <param name="size">Size of the byte array to be created.</param>
        /// <returns>A byte array filled with zeros.</returns>
        public static byte[] BuildZero(int size) => new byte[size];

        /// <summary>
        /// Sorts a 2D integer array based on the values in the second row.
        /// </summary>
        /// <param name="list">The 2D array to sort.</param>
        /// <param name="size">Size of the array (both dimensions are assumed to be of this size).</param>
        /// <returns>A sorted 2D integer array.</returns>
        public static int[,] Sort(int[,] list, int size)
        {
            for (int j = 0; j < size; j++)
            {
                int maxIndex = 0;

                // Find the max value in the current slice of the array
                for (int i = 0; i < size - j; i++)
                {
                    if (list[1, i] > list[1, maxIndex])
                    {
                        maxIndex = i;
                    }
                }

                // Swap the max value with the last value in the current slice
                Swap(ref list[0, size - j - 1], ref list[0, maxIndex]);
                Swap(ref list[1, size - j - 1], ref list[1, maxIndex]);
            }
            return list;
        }

        /// <summary>
        /// Sorts a 1D integer array in descending order.
        /// </summary>
        /// <param name="list">The 1D array to sort.</param>
        /// <param name="size">Size of the array.</param>
        /// <returns>A sorted integer array.</returns>
        public static int[] Sort(int[] list, int size)
        {
            return list.OrderByDescending(x => x).ToArray();
        }

        /// <summary>
        /// Compares two byte arrays for equality.
        /// </summary>
        /// <param name="b1">First byte array.</param>
        /// <param name="b2">Second byte array.</param>
        /// <returns>True if the arrays are equal, otherwise false.</returns>
        public static bool ByteArrayCompare(byte[] b1, byte[] b2) => b1.SequenceEqual(b2);

        /// <summary>
        /// Swaps the values of two integers.
        /// </summary>
        /// <param name="x">First integer.</param>
        /// <param name="y">Second integer.</param>
        private static void Swap(ref int x, ref int y)
        {
            int temp = x;
            x = y;
            y = temp;
        }
    }
}
