namespace nfs2iso2nfs.Helpers
{
    public class NfsHelper
    {
        public static void EncryptNFS(string inFile, string outFile, byte[] key, int size, byte[] header)
        {
            ProcessFile(inFile, outFile, key, size, header, true);
        }

        public static void DecryptNFS(string inFile, string outFile, byte[] key, int size)
        {
            ProcessFile(inFile, outFile, key, size);
        }

        private static void ProcessFile(string inFile, string outFile, byte[] key, int size, byte[]? header = null, bool encrypt = false)
        {
            using var er = new BinaryReader(File.OpenRead(inFile));
            using var ew = new BinaryWriter(File.OpenWrite(outFile));

            if (header != null)
            {
                ew.Write(header);
            }

            CryptNFS(er, ew, key, size, encrypt);
        }

        private static void CryptNFS(BinaryReader er, BinaryWriter ew, byte[] key, int size, bool encrypt = false)
        {
            // Initialize Initialization Vectors (IVs)
            var iv = ByteHelper.BuildZero(key.Length);
            var block_iv = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F, 0x00 };

            var remainingSize = er.BaseStream.Length;
            var timer = 0;

            while (remainingSize > 0)
            {
                var chunkSize = remainingSize > size ? size : (int)remainingSize;

                var sector = er.ReadBytes(chunkSize);

                // Determine which IV to use based on position in the file.
                if (ew.BaseStream.Position >= 0x18000)
                {
                    iv = block_iv;
                }

                sector = KeyHelper.CryptAes128Cbc(KeyHelper.CreateAes128Cbc(key, iv), sector, encrypt);

                // If encrypting the game partition, update the IV.
                if (ew.BaseStream.Position >= 0x18000)
                {
                    block_iv = IncrementBlockIV(block_iv);
                }

                ew.Write(sector);
                remainingSize -= size;
                timer++;

                if (timer >= 8000)
                {
                    timer = 0;
                }
            }
        }

        private static byte[] IncrementBlockIV(byte[] block_iv)
        {
            if (++block_iv[15] == 0)
            {
                if (++block_iv[14] == 0)
                {
                    if (++block_iv[13] == 0)
                    {
                        block_iv[12]++;
                    }
                }
            }

            return block_iv;
        }

        public static long[] EncryptManipulateIso(string inFile, string outFile, int sectorSize, byte[] commonKey)
        {
            return ManipulateIso(inFile, outFile, sectorSize, commonKey);
        }

        public static void DecryptManipulateIso(string inFile, string outFile, int sectorSize, byte[] commonKey)
        {
            ManipulateIso(inFile, outFile, sectorSize, commonKey, true);
        }

        private static long[] ManipulateIso(string inFile, string outFile, int sectorSize, byte[] commonKey, bool enc = false)
        {
            using var er = new BinaryReader(File.OpenRead(inFile));
            using var ew = new BinaryWriter(File.OpenWrite(outFile));

            long[] sizeInfo = new long[2];
            ew.Write(er.ReadBytes(0x40000));

            var partitionTable = er.ReadBytes(0x20);
            ew.Write(partitionTable);
            int[,] partitionInfo = new int[2, 4];            //first coorfinate number of partitions, second offset of partition table

            for (byte i = 0; i < 4; i++)
            {
                partitionInfo[0, i] = partitionTable[0x0 + 0x8 * i] * 0x1000000 + partitionTable[0x1 + 0x8 * i] * 0x10000 + partitionTable[0x2 + 0x8 * i] * 0x100 + partitionTable[0x3 + 0x8 * i];
                if (partitionInfo[0, i] == 0)
                    partitionInfo[1, i] = 0;
                else partitionInfo[1, i] = (partitionTable[0x4 + 0x8 * i] * 0x1000000 + partitionTable[0x5 + 0x8 * i] * 0x10000 + partitionTable[0x6 + 0x8 * i] * 0x100 + partitionTable[0x7 + 0x8 * i]) * 0x4;
            }
            partitionInfo = ByteHelper.Sort(partitionInfo, 4);

            byte[][] partitionInfoTable = new byte[4][];
            var partitionOffsetList = new List<int>();
            long curPos = 0x40020;
            var k = 0;

            for (var i = 0; i < 4; i++)
            {
                if (partitionInfo[0, i] != 0)
                {
                    ew.Write(er.ReadBytes((int)(partitionInfo[1, i] - curPos)));
                    curPos += (partitionInfo[1, i] - curPos);
                    partitionInfoTable[i] = er.ReadBytes(0x8 * partitionInfo[0, i]);
                    curPos += (0x8 * partitionInfo[0, i]);
                    for (var j = 0; j < partitionInfo[0, i]; j++)
                        if (partitionInfoTable[i][0x7 + 0x8 * j] == 0) //check if game partition
                        {
                            partitionOffsetList.Add((partitionInfoTable[i][0x0 + 0x8 * j] * 0x1000000 + partitionInfoTable[i][0x1 + 0x8 * j] * 0x10000 + partitionInfoTable[i][0x2 + 0x8 * j] * 0x100 + partitionInfoTable[i][0x3 + 0x8 * j]) * 0x4);
                            k++;
                        }
                    ew.Write(partitionInfoTable[i]);
                }
            }
            var partitionOffsets = partitionOffsetList.ToArray();
            partitionOffsets = ByteHelper.Sort(partitionOffsets, partitionOffsets.Length);
            sizeInfo[0] = partitionOffsets[0];

            var iv = new byte[0x10];
            var decHashTable = new byte[0x400];
            var encHashTable = new byte[0x400];

            for (var i = 0; i < partitionOffsets.Length; i++)
            {
                ew.Write(er.ReadBytes((int)(partitionOffsets[i] - curPos)));
                curPos += (partitionOffsets[i] - curPos);
                ew.Write(er.ReadBytes(0x1BF));                              //Write start of partiton

                var enc_titlekey = er.ReadBytes(0x10);                   //read encrypted titlekey
                ew.Write(enc_titlekey);                                     //Write encrypted titlekey
                ew.Write(er.ReadBytes(0xD));                                //Write bytes till titleID

                var titleID = er.ReadBytes(0x8);                         //read titleID
                ew.Write(titleID);

                for (int j = 0; j < 0x10; j++)
                    if (j < 8)
                        iv[j] = titleID[j];
                    else iv[j] = 0x0;

                ew.Write(er.ReadBytes(0xC0));                               //Write bytes till end of ticket
                var partitionHeader = er.ReadBytes(0x1FD5C);
                var partitionSize = (long)0x4 * (partitionHeader[0x18] * 0x1000000 + partitionHeader[0x19] * 0x10000 + partitionHeader[0x1A] * 0x100 + partitionHeader[0x1B]);

                ew.Write(partitionHeader);                                  //Write bytes till start of partition data
                curPos += 0x20000;
                curPos += partitionSize;

                var titleKey = KeyHelper.CryptAes128Cbc(KeyHelper.CreateAes128Cbc(commonKey, iv), enc_titlekey);
                var Sector = new byte[sectorSize];

                //NFS to ISO
                //ISO to NFS
                while (partitionSize >= sectorSize)
                {
                    Array.Clear(iv, 0, 0x10);                                                // clear IV for encrypting hash table
                    if (enc)
                    {
                        decHashTable = er.ReadBytes(0x400);                                      // read raw hash table from nfs
                        encHashTable = KeyHelper.CryptAes128Cbc(KeyHelper.CreateAes128Cbc(titleKey, iv), decHashTable, enc);            // encrypt table
                        ew.Write(encHashTable);                                                  // write encrypted hash table to iso
                    }
                    else
                    {
                        encHashTable = er.ReadBytes(0x400);                                      // read encrypted hash table from iso
                        decHashTable = KeyHelper.CryptAes128Cbc(KeyHelper.CreateAes128Cbc(titleKey, iv), encHashTable, enc);           // decrypt table
                        ew.Write(decHashTable);                                                  // write decrypted hash table to nfs
                    }

                    //quit the loop if already at the end of input file or beyond (avoid the crash)
                    if (er.BaseStream.Position >= er.BaseStream.Length)
                        break;

                    Array.Copy(encHashTable, 0x3D0, iv, 0, 0x10);                            // get IV for encrypting the rest
                    Sector = er.ReadBytes(sectorSize - 0x400);
                    Sector = KeyHelper.CryptAes128Cbc(KeyHelper.CreateAes128Cbc(titleKey, iv), Sector, enc);                         // encrypt the remaining bytes

                    ew.Write(Sector);
                    partitionSize -= sectorSize;
                }
                sizeInfo[1] = curPos - sizeInfo[0];
            }
            if (enc)
            {
                var num = 0x118240000;
                long rest = (curPos > num ? 0x1FB4E0000 : num) - curPos;

                while (rest > 0)
                {
                    ew.Write(ByteHelper.BuildZero(rest > sectorSize ? sectorSize : (int)rest));
                    rest -= sectorSize;
                }
            }
            return sizeInfo;
        }

    }
}
