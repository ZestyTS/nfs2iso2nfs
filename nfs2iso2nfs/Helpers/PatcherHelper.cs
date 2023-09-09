using nfs2iso2nfs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nfs2iso2nfs.Helpers
{
    public class PatcherHelper
    {
        private const int PatternLength4 = 4;
        private const int PatternLength6 = 6;
        private const int PatternLength8 = 8;
        private const int PatternLength12 = 12;
        private const int PatternLength16 = 16;

        private static long FindPatternInStream(AsyncMemoryStream stream, byte[] pattern)
        {
            byte[] buffer = new byte[pattern.Length];
            for (long offset = 0; offset < stream.Length - pattern.Length + 1; offset++)
            {
                stream.Position = offset;
                stream.Read(buffer, 0, pattern.Length);
                if (ByteHelper.ByteArrayCompare(buffer, pattern))
                    return offset;
            }
            return -1;
        }

        private static async Task<int> ApplyPatchAsync(AsyncMemoryStream inputIos, byte[] pattern, Func<AsyncMemoryStream, int, Task> patchAction, int patchOffset = 0)
        {
            byte[] buffer = new byte[pattern.Length];
            int patchCount = 0;

            for (int offset = 0; offset < inputIos.Length - pattern.Length; offset++)
            {
                inputIos.Seek(offset, SeekOrigin.Begin); // Set the position
                await inputIos.ReadAsync(buffer, 0, pattern.Length);

                if (ByteHelper.ByteArrayCompare(buffer, pattern))
                {
                    int position = offset + patchOffset;
                    await patchAction(inputIos, position);
                    patchCount++;
                }
            }

            return patchCount;
        }



        public static bool CheckIfRev509(AsyncMemoryStream inputIos)
        {
            byte[] revPattern = { 0x73, 0x76, 0x6E, 0x2D };
            var position = FindPatternInStream(inputIos, revPattern);
            if (position >= 0)
            {
                byte[] revisionBuffer = ReadFromStream(inputIos, 4);
                return Encoding.UTF8.GetString(revisionBuffer) == "r590";
            }
            return false;
        }
        public static async Task DontKeepLegitAsync(AsyncMemoryStream inputIos)
        {
            var patterns = new List<byte[]>
            {
                new byte[] { 0x20, 0x07, 0x23, 0xA2 },
                new byte[] { 0x20, 0x07, 0x4B, 0x0B }
            };

            foreach (var pattern in patterns)
                await ApplyPatchAsync(inputIos, pattern, async (stream, position) => await stream.WriteByteAsync(0x00));
        }

        public static async Task MapShoulderToTriggerAsync(AsyncMemoryStream inputIos)
        {
            var patchList = new List<(byte[] pattern, byte[] patch)>
            {
                (new byte[] { 0x40, 0x05, 0x46, 0xA9 }, new byte[] { 0x26, 0x80, 0x40, 0x06 }),
                (new byte[] { 0x1C, 0x05, 0x40, 0x35 }, new byte[] { 0x25, 0x40, 0x40, 0x05 }),
                (new byte[] { 0x23, 0x7F, 0x1C, 0x02 }, new byte[] { 0x46, 0xB1, 0x23, 0x20, 0x40, 0x03 }),
                (new byte[] { 0x46, 0x53, 0x42, 0x18 }, new byte[] { 0x23, 0x10, 0x40, 0x03 }),
                (new byte[] { 0x1C, 0x05, 0x80, 0x22 }, new byte[] { 0x25, 0x40, 0x80, 0x22, 0x40, 0x05 })
            };

            foreach (var (pattern, patch) in patchList)
            {
                await ApplyPatchAsync(inputIos, pattern, async (stream, position) =>
                {
                    stream.Seek(position, SeekOrigin.Begin);
                    await stream.WriteAsync(patch, 0, patch.Length);
                });
            }
        }

        public static async Task EnableWiiRemoteEmulationAsync(AsyncMemoryStream inputIos)
        {
            byte[] pattern = { 0x16, 0x13, 0x1C, 0x02, 0x40, 0x9A, 0x1C, 0x13 };
            byte[] patch = { 0x23, 0x00 };

            await ApplyPatchAsync(inputIos, pattern, async (stream, position) =>
            {
                stream.Seek(position, SeekOrigin.Begin);
                await stream.WriteAsync(patch, 0, 2);
            });
        }

        public static async Task EnableProperInputInHomebrewAsync(AsyncMemoryStream inputIos)
        {
            var patchList = new List<(byte[] pattern, byte[] patch)>
    {
        (new byte[] { 0xD0, 0x0B, 0x23, 0x08, 0x43, 0x13, 0x60, 0x0B }, new byte[] { 0x46, 0xC0 }),
        (new byte[] { 0x01, 0x94, 0xB5, 0x00, 0x4B, 0x08, 0x22, 0x01 }, new byte[] { 0x22, 0x00 })
    };

            foreach (var (pattern, patch) in patchList)
            {
                await ApplyPatchAsync(inputIos, pattern, async (stream, position) =>
                {
                    stream.Seek(position, SeekOrigin.Begin);
                    await stream.WriteAsync(patch);
                });
            }

            byte[] patternNintendont1 = { 0xB0, 0xBA, 0x1C, 0x0F };
            byte[] patchNintendont1 = { 0xE5, 0x9F, 0x10, 0x04, 0xE5, 0x91, 0x00, 0x00, 0xE1, 0x2F, 0xFF, 0x10, 0x12, 0xFF, 0xFF, 0xE0 };
            await ApplyPatchAsync(inputIos, patternNintendont1, async (stream, position) =>
            {
                stream.Seek(position - 12, SeekOrigin.Begin);
                await stream.WriteAsync(patchNintendont1);
            });

            byte[] patternNintendont2 = { 0x68, 0x4B, 0x2B, 0x06 };
            byte[] patchNintendont2 = { 0x49, 0x01, 0x47, 0x88, 0x46, 0xC0, 0xE0, 0x01, 0x12, 0xFF, 0xFE, 0x00, 0x22, 0x00, 0x23, 0x01, 0x46, 0xC0, 0x46, 0xC0 };
            await ApplyPatchAsync(inputIos, patternNintendont2, async (stream, position) =>
            {
                stream.Seek(position, SeekOrigin.Begin);
                await stream.WriteAsync(patchNintendont2);
            });

            byte[] buffer8 = new byte[8];
            byte[] buffer4 = new byte[4];

            byte[] patternNintendont3a = { 0x0D, 0x80, 0x00, 0x00, 0x0D, 0x80, 0x00, 0x00 };
            byte[] patternNintendont3b = { 0x00, 0x00, 0x00, 0x02 };
            byte[] patchNintendont3 = { 0x00, 0x00, 0x00, 0x03 };

            for (int offset = 0; offset < inputIos.Length - 8; offset++)
            {
                inputIos.Position = offset;
                await inputIos.ReadAsync(buffer8.AsMemory(0, 8));

                if (ByteHelper.ByteArrayCompare(buffer8, patternNintendont3a))
                {
                    int position = offset + 0x10;
                    await ApplyPatchAsync(inputIos, patternNintendont3b, async (stream, _) =>
                    {
                        stream.Seek(position, SeekOrigin.Begin);
                        await stream.WriteAsync(patchNintendont3.AsMemory(0, 4));
                    });
                }
            }
        }


        public static async Task<int> EnableHorizontalWiiRemoteEmulationAsync(AsyncMemoryStream inputIos)
        {
            byte[] pattern = { 0x4A, 0x71, 0x42, 0x13, 0xD0, 0xD2, 0x9B, 0x00 };

            int appliedPatches = await ApplyMultiplePatchesAsync(inputIos, pattern, async (stream, position) =>
            {
                var patches = new List<(int offset, byte value)>
                {
                    (0x07, 0x02), // dpad left -> down
                    (0x0F, 0x03), // dpad right -> up
                    (0x1D, 0x01), // dpad down -> right
                    (0x2B, 0x00), // dpad up -> left
                    (0x65, 0x07), // B -> 2
                    (0x75, 0x06), // A -> 1
                    (0x85, 0x04), // 1 -> B
                    (0x95, 0x05)  // 2 -> A
                };

                foreach (var (offset, value) in patches)
                {
                    stream.Seek(position + offset, SeekOrigin.Begin);
                    await stream.WriteByteAsync(value);
                }
            });

            return appliedPatches;
        }

        public static async Task<int> ApplyMultiplePatchesAsync(AsyncMemoryStream stream, byte[] pattern, Func<AsyncMemoryStream, int, Task> patchAction, int patchOffset = 0)
        {
            int patchCount = 0;
            for (int offset = 0; offset < stream.Length; offset++)
            {
                stream.Position = offset;
                byte[] buffer = new byte[pattern.Length];
                await stream.ReadAsync(buffer, 0, pattern.Length);

                if (ByteHelper.ByteArrayCompare(buffer, pattern))
                {
                    int position = offset + patchOffset;
                    stream.Seek(position, SeekOrigin.Begin);
                    await patchAction(stream, position);
                    patchCount++;
                }
            }

            return patchCount;
        }


        public static async Task WiiMotePassthroughAsync(AsyncMemoryStream inputIos)
        {
            var patchList = new List<(byte[] pattern, byte[] patch, int patchOffset)>
            {
                (new byte[] { 0x20, 0x4B, 0x01, 0x68, 0x18, 0x47, 0x70, 0x00 }, new byte[] { 0x20, 0x00 }, 3),
                (new byte[] { 0x28, 0x00, 0xD0, 0x03, 0x49, 0x02, 0x22, 0x09 }, new byte[] { 0xF0, 0x04, 0xFF, 0x21, 0x48, 0x02, 0x21, 0x09, 0xF0, 0x04, 0xFE, 0xF9 }, 0),
                (new byte[] { 0xF0, 0x01, 0xFA, 0xB9 }, new byte[] { 0xF7, 0xFC, 0xFB, 0x95 }, 0)
            };

            foreach (var (pattern, patch, patchOffset) in patchList)
            {
                await ApplyPatchAsync(inputIos, pattern, async (stream, position) =>
                {
                    stream.Seek(position, SeekOrigin.Begin);
                    await stream.WriteAsync(patch);
                }, patchOffset);
            }
        }

        public static async Task InstantCCAsync(AsyncMemoryStream inputIos)
        {
            byte[] pattern = { 0x78, 0x93, 0x21, 0x10, 0x2B, 0x02, 0xD1, 0xB7 };
            byte[] patch = { 0x78, 0x93, 0x21, 0x10, 0x2B, 0x02, 0x46, 0xC0 };
            await ApplyPatchAsync(inputIos, pattern, async (stream, position) => { await stream.WriteAsync(patch, 0, patch.Length); });
        }

        public static async Task NoCCAsync(AsyncMemoryStream inputIos)
        {
            byte[] pattern = { 0x78, 0x93, 0x21, 0x10, 0x2B, 0x02, 0xD1, 0xB7 };
            byte[] patch = { 0x78, 0x93, 0x21, 0x10, 0x2B, 0x02, 0xE0, 0xB7 };
            await ApplyPatchAsync(inputIos, pattern, async (stream, position) => { await stream.WriteAsync(patch, 0, patch.Length); });
        }

        // Private helper methods...

        private static byte[] ReadFromStream(AsyncMemoryStream stream, int count)
        {
            byte[] buffer = new byte[count];
            stream.Read(buffer, 0, count);
            return buffer;
        }
    }
}
