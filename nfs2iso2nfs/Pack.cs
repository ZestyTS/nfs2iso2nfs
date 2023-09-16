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
        private bool _enc = false;
        private bool _keepFiles = false;
        private string _keyFile = ".." + Path.DirectorySeparatorChar + "code" + Path.DirectorySeparatorChar + "htk.bin";
        private string _isoFile = "game.iso";
        private string _wiiKeyFile = "wii_common_key.bin";
        public ILogger<Pack>? Logger { get; set; }

        public Pack(ILogger<Pack>? logger = null)
        {
            Logger = logger;
        }

        public async Task Convert(string[] args)
        {
            _nfs = new Nfs();

            if (!CheckArgs(args))
                return;

            SetupFiles();

            if (!ArgValidation())
                return;

            _nfs.Key = await GetKeyAsync() ?? Array.Empty<byte>();
            if (_nfs?.Key == null)
                return;

            if (_enc)
                await EncryptAsync();
            else
                await DecryptAsync();

            if (!_keepFiles)
            {
                Logger?.LogInformation("Deleting files!");
                await _nfs.DeleteFilesAsync();
                Logger?.LogInformation("Deleted!");
                 
            }
        }

        private async Task DecryptAsync()
        {
            var header = await ByteHelper.GetHeaderAsync(_nfs.Dir + Path.DirectorySeparatorChar + "hif_000000.nfs");
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
            NfsHelper.DecryptManipulateIso(_nfs.HifUnpack, _isoFile, _nfs.SectorSize, _nfs.CommonKey);
            Logger?.LogInformation("Felt up Iso");
             
        }

        private async Task EncryptAsync()
        {
            Logger?.LogInformation("Do Patching if applicable!");
            await _patch.DoThePatchingAsync();
            Logger?.LogInformation("Patching Done!");
             

            Logger?.LogInformation("Manipulate Iso - Encrypt");
            var size = NfsHelper.EncryptManipulateIso(_isoFile, _nfs.HifUnpack, _nfs.SectorSize, _nfs.CommonKey);
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

        private bool CheckArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-dec":
                        _enc = false;
                        break;
                    case "-enc":
                        _enc = true;
                        break;
                    case "-keep":
                        _keepFiles = true;
                        break;
                    case "-legit":
                        _patch.KeepLegit = true;
                        break;
                    case "-key":
                        if (i == args.Length)
                            return false;
                        _keyFile = args[i + 1];
                        i++;
                        break;
                    case "-wiikey":
                        if (i == args.Length)
                            return false;
                        _wiiKeyFile = args[i + 1];
                        i++;
                        break;
                    case "-iso":
                        if (i == args.Length)
                            return false;
                        _isoFile = args[i + 1];
                        i++;
                        break;
                    case "-nfs":
                        if (i == args.Length)
                            return false;
                        _nfs.Dir = args[i + 1];
                        i++;
                        break;
                    case "-fwimg":
                        if (i == args.Length)
                            return false;
                        _patch.FwFile = args[i + 1];
                        i++;
                        break;
                    case "-lrpatch":
                        _patch.MapShoulderToTrigger = true;
                        break;
                    case "-wiimote":
                        _patch.VertWiimote = true;
                        break;
                    case "-horizontal":
                        _patch.HorizWiimote = true;
                        break;
                    case "-homebrew":
                        _patch.Homebrew = true;
                        break;
                    case "-passthrough":
                        _patch.PassThrough = true;
                        break;
                    case "-instantcc":
                        _patch.InstantCC = true;
                        break;
                    case "-nocc":
                        _patch.NoCC = true;
                        break;
                    case "-output":
                        _nfs.NfsOutputDirectory = args[i + 1];
                        break;

                    case "-help":
                        Logger?.LogInformation("+++++ NFS2ISO2NFS v0.6 +++++");
                         
                        Logger?.LogInformation("-dec            Decrypt .nfs files to an .iso file.");
                        Logger?.LogInformation("-enc            Encrypt an .iso file to .nfs file(s)");
                        Logger?.LogInformation("-key <file>     Location of AES key file. DEFAULT: code" + Path.DirectorySeparatorChar + "htk.bin.");
                        Logger?.LogInformation("-wiikey <file>  Location of Wii Common key file. DEFAULT: wii_common_key.bin.");
                        Logger?.LogInformation("-iso <file>     Location of .iso file. DEFAULT: game.iso.");
                        Logger?.LogInformation("-nfs <file>     Location of .nfs files. DEFAULT: current Directory.");
                        Logger?.LogInformation("-fwimg <file>   Location of fw.img. DEFAULT: code" + Path.DirectorySeparatorChar + "fw.img.");
                        Logger?.LogInformation("-keep           Don't delete the files produced in intermediate steps.");
                        Logger?.LogInformation("-legit          Don't patch fw.img to allow fakesigned content");
                        Logger?.LogInformation("-lrpatch        Map emulated Classic Controller's L & R to Gamepad's ZL & ZR");
                        Logger?.LogInformation("-wiimote        Emulate a Wii Remote instead of the Classic Controller");
                        Logger?.LogInformation("-horizontal     Remap Wii Remote d-pad for horizontal usage (implies -wiimote)");
                        Logger?.LogInformation("-homebrew       Various patches to enable proper homebrew functionality");
                        Logger?.LogInformation("-passthrough    Allow homebrew to keep using normal wiimotes with gamepad enabled");
                        Logger?.LogInformation("-instantcc      Report emulated Classic Controller at the very first check");
                        Logger?.LogInformation("-nocc           Report that no Classic Controller is connected");
                        Logger?.LogInformation("-output         Location of where the NFS files will be outputted to. DEFAULT: code" + Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);
                        Logger?.LogInformation("-help           Print this text.");
                        return false;
                    default:
                        break;
                }
            }
            return true;
        }

        private void SetupFiles()
        {
            string dir = Directory.GetCurrentDirectory();
            if (!Path.IsPathRooted(_keyFile))
                _keyFile = dir + Path.DirectorySeparatorChar + _keyFile;
            if (!Path.IsPathRooted(_isoFile))
                _isoFile = dir + Path.DirectorySeparatorChar + _isoFile;
            if (!Path.IsPathRooted(_wiiKeyFile))
                _wiiKeyFile = dir + Path.DirectorySeparatorChar + _wiiKeyFile;
            if (!Path.IsPathRooted(_nfs.Dir))
                _nfs.Dir = dir + Path.DirectorySeparatorChar + _nfs.Dir;
            if (!Path.IsPathRooted(_patch.FwFile))
                _patch.FwFile = dir + Path.DirectorySeparatorChar + _patch.FwFile;
        }

        private bool ArgValidation()
        {
            if (_patch.MapShoulderToTrigger && (_patch.HorizWiimote || _patch.VertWiimote))
            {
                Logger?.LogInformation("ERROR: Please don't mix patches for Classic Controller and Wii Remote.");
                return false;
            }

            if (!_enc && File.Exists(_nfs.Dir + Path.DirectorySeparatorChar + "hif_000000.nfs"))
            {
                Logger?.LogInformation("+++++ NFS2ISO +++++");

                if (!_enc && !File.Exists(_nfs.Dir + Path.DirectorySeparatorChar + "hif_000000.nfs"))
                {
                    Logger?.LogInformation("ERROR: .nfs files not found! Exiting...");
                    return false;
                }
                else if (!_enc && File.Exists(_nfs.Dir + Path.DirectorySeparatorChar + "hif_000000.nfs"))
                {
                    Logger?.LogInformation("You haven't specified if you want to use nfs2iso or iso2nfs");
                    Logger?.LogInformation("Found .nfs files! Assuming you want to use nfs2iso...");
                    _enc = false;
                }
            }
            else if (_enc && File.Exists(_isoFile))
            {
                Logger?.LogInformation("+++++ ISO2NFS +++++");

                if (_enc && !File.Exists(_isoFile))
                {
                    Logger?.LogInformation("ERROR: .iso file not found! Exiting...");
                    return false;
                }
                if (_enc && !File.Exists(_patch.FwFile))
                {
                    Logger?.LogInformation("ERROR: fw.img not found! Exiting...");
                    return false;
                }
                else if (_enc && File.Exists(_isoFile))
                {
                    Logger?.LogInformation("You haven't specified if you want to use nfs2iso or iso2nfs");
                    Logger?.LogInformation("Found .iso file!  Assuming you want to use iso2nfs...");
                    _enc = true;
                }
            }
            else
            {
                Logger?.LogInformation("You haven't specified if you want to use nfs2iso or iso2nfs");
                Logger?.LogInformation("Found neither .iso nor .nfs files! Check -help for usage of this program.");
                return false;
            }
            return true;
        }

        private async Task<byte[]?> GetKeyAsync()
        {
            Logger?.LogInformation("Searching for AES key file...");
            if (!File.Exists(_keyFile))
            {
                Logger?.LogInformation("ERROR: Could not find AES key file! Exiting...");
                return null;
            }
            var key = await KeyHelper.GetKeyAsync(_keyFile);
            if (key == null)
            {
                Logger?.LogInformation("ERROR: AES key file has wrong file size! Exiting...");
                return null;
            }
            Logger?.LogInformation("AES key file found!");

            if (_nfs.CommonKey[0] != 0xeb)
            {
                Logger?.LogInformation("Wii common key not found in source code. Looking for file...");
                if (!File.Exists(_wiiKeyFile))
                {
                    _nfs.CommonKey = ConvertHexStringToByteArray("ebe42a225e8593e448d9c5457381aaf7");
                    if (_nfs.CommonKey[0] != 0xeb)
                    {
                        Logger?.LogInformation("ERROR: Could not find Wii common key file! Exiting...");
                        return null;
                    }
                    else
                        Logger?.LogInformation("Wii common key has been found.");
                }
                else
                {
                    _nfs.CommonKey = await KeyHelper.GetKeyAsync(_wiiKeyFile) ?? Array.Empty<byte>();
                    if (_nfs.CommonKey == null)
                    {
                        Logger?.LogInformation("ERROR: Wii common key file has wrong file size! Exiting...");
                        return null;
                    }
                    Logger?.LogInformation("Wii Common Key file found!");
                }
            }
            else
                Logger?.LogInformation("Wii common key found in source code!");

            return key;
        }


        public byte[] ConvertHexStringToByteArray(string hexString)
        {
            if (hexString.Length % 2 != 0)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));

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
