using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nfs2iso2nfs;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Initialize configuration values (example values here, replace with actual values)
            var htk = "path/to/htk.bin";
            var output = "path/to/output";
            var fwImg = "path/to/fw.img";
            var iso = "path/to/iso.iso";

            // Create the configuration object with the values
            
            var config = new PackConfiguration
            {
                IsEncrypted = true,        // Set to true for encryption, false for decryption
                KeepFiles = false,         // Set to true if you want to keep intermediate files
                KeyFilePath = htk,         // Path to the AES key file
                IsoFilePath = iso,         // Path to the ISO file
                WiiKeyFilePath = "path/to/wii_common_key.bin", // Path to the Wii key file
                NfsDirectory = "path/to/nfs_dir", // Directory where NFS files are located
                FwImageFilePath = fwImg,   // Path to the firmware image
                HomebrewPatches = true,    // Enable homebrew patches
                OutputDirectory = output   // Set the output directory for the NFS files
            };

            // Create a logger (optional, can be set to null)
            using var serviceProvider = new ServiceCollection()
                .AddLogging(configure => configure.AddConsole())
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILogger<Pack>>();

            // Create an instance of the Pack class with the config and logger
            var packInstance = new Pack(config, logger);

            // Run the conversion asynchronously
            packInstance.ConvertAsync().GetAwaiter().GetResult();
        }
    }
}
