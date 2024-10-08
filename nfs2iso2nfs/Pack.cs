using nfs2iso2nfs.Helpers;
using nfs2iso2nfs.Models;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace nfs2iso2nfs
{
    public class Pack
    {
        private Patch _patch = new("");
        private Nfs _nfs = new();
        public ILogger<Pack>? Logger { get; set; }
        public PackConfiguration Config { get; set; }

        public Pack(PackConfiguration config, ILogger<Pack>? logger = null)
        {
            Config = config;
            Logger = logger;
        }

        public async Task ConvertAsync()
        {
            SetupFiles();

            if (!ValidateConfiguration())
                return;

            _nfs.Key = await GetKeyAsync() ?? Array.Empty<byte>();
            if (_nfs?.Key == null)
                return;

            if (Config.IsEncrypted)
                await EncryptAsync();
            else
                await DecryptAsync();

            if (!Config.KeepFiles)
            {
                Logger?.LogInformation("Deleting files!");
                await _nfs.DeleteFilesAsync();
                Logger?.LogInformation("Deleted!");
            }
        }

        private async Task DecryptAsync()
        {
            var header = await ByteHelper.GetHeaderAsync(Path.Combine(_nfs.Dir, "hif_000000.nfs"));
            Logger?.LogInformation("Combining NFS Files");
            await _nfs.CombineNFSFilesAsync();
            Logger?.LogInformation("Combined");

            Logger?.LogInformation("Decrypting hif.nfs...");
            NfsHelper.DecryptNFS(_nfs.Hif, _nfs.HifDec, _nfs.Key, _nfs.SectorSize);
            Logger?.LogInformation("Decrypted!");

            Logger?.LogInformation("Unpacking nfs");
            _nfs.Unpack(header);
            Logger?.LogInformation("Unpacked");

            Logger?.LogInformation("Manipulate Iso - Decrypt");
            NfsHelper.DecryptManipulateIso(_nfs.HifUnpack, Config.IsoFilePath, _nfs.SectorSize, _nfs.CommonKey);
            Logger?.LogInformation("Felt up Iso");
        }

        private async Task EncryptAsync()
        {
            Logger?.LogInformation("Do Patching if applicable!");
            await _patch.DoThePatchingAsync();
            Logger?.LogInformation("Patching Done!");

            Logger?.LogInformation("Manipulate Iso - Encrypt");
            var size = NfsHelper.EncryptManipulateIso(Config.IsoFilePath, _nfs.HifUnpack, _nfs.SectorSize, _nfs.CommonKey);
            Logger?.LogInformation("Felt up Iso");

            Logger?.LogInformation("Packing nfs");
            var header = _nfs.PackNFS(size);
            Logger?.LogInformation("Packing complete!");

            Logger?.LogInformation("EncryptNFS");
            NfsHelper.EncryptNFS(_nfs.HifDec, _nfs.Hif, _nfs.Key, _nfs.SectorSize, header);
            Logger?.LogInformation("Encrypted!");

            Logger?.LogInformation("Split NFS File");
            await _nfs.SplitFileAsync();
            Logger?.LogInformation("Splitted!");
        }

        private void SetupFiles()
        {
            string dir = Directory.GetCurrentDirectory();
            Config.KeyFilePath = Path.IsPathRooted(Config.KeyFilePath) ? Config.KeyFilePath : Path.Combine(dir, Config.KeyFilePath);
            Config.IsoFilePath = Path.IsPathRooted(Config.IsoFilePath) ? Config.IsoFilePath : Path.Combine(dir, Config.IsoFilePath);
            Config.WiiKeyFilePath = Path.IsPathRooted(Config.WiiKeyFilePath) ? Config.WiiKeyFilePath : Path.Combine(dir, Config.WiiKeyFilePath);
            _nfs.Dir = Path.IsPathRooted(Config.NfsDirectory) ? Config.NfsDirectory : Path.Combine(dir, Config.NfsDirectory);
            _patch.FwFile = Path.IsPathRooted(Config.FwImageFilePath) ? Config.FwImageFilePath : Path.Combine(dir, Config.FwImageFilePath);
        }

        private bool ValidateConfiguration()
        {
            if (Config.MapShoulderToTrigger && (Config.HorizontalWiimote || Config.VerticalWiimote))
            {
                Logger?.LogInformation("ERROR: Please don't mix patches for Classic Controller and Wii Remote.");
                return false;
            }

            if (!Config.IsEncrypted && File.Exists(Path.Combine(_nfs.Dir, "hif_000000.nfs")))
            {
                Logger?.LogInformation("Found .nfs files! Assuming you want to use nfs2iso...");
                Config.IsEncrypted = false;
            }
            else if (Config.IsEncrypted && File.Exists(Config.IsoFilePath))
            {
                Logger?.LogInformation("Found .iso file!  Assuming you want to use iso2nfs...");
                Config.IsEncrypted = true;
            }
            else
            {
                Logger?.LogInformation("Found neither .iso nor .nfs files! Check documentation for usage.");
                return false;
            }

            return true;
        }

        private async Task<byte[]?> GetKeyAsync()
        {
            Logger?.LogInformation("Searching for AES key file...");
            if (!File.Exists(Config.KeyFilePath))
            {
                Logger?.LogInformation("ERROR: Could not find AES key file! Exiting...");
                return null;
            }

            var key = await KeyHelper.GetKeyAsync(Config.KeyFilePath);
            if (key == null)
            {
                Logger?.LogInformation("ERROR: AES key file has wrong file size! Exiting...");
                return null;
            }
            Logger?.LogInformation("AES key file found!");

            if (_nfs.CommonKey[0] != 0xeb)
            {
                Logger?.LogInformation("Wii common key not found in source code. Looking for file...");
                if (!File.Exists(Config.WiiKeyFilePath))
                {
                    _nfs.CommonKey = ConvertHexStringToByteArray("ebe42a225e8593e448d9c5457381aaf7");
                    if (_nfs.CommonKey[0] != 0xeb)
                    {
                        Logger?.LogInformation("ERROR: Could not find Wii common key file! Exiting...");
                        return null;
                    }
                }
                else
                {
                    _nfs.CommonKey = await KeyHelper.GetKeyAsync(Config.WiiKeyFilePath) ?? Array.Empty<byte>();
                }
            }

            return key;
        }

        public byte[] ConvertHexStringToByteArray(string hexString)
        {
            if (hexString.Length % 2 != 0)
                throw new ArgumentException($"The binary key cannot have an odd number of digits: {hexString}");

            var data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++)
            {
                var byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
        }
    }
}
