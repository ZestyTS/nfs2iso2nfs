namespace nfs2iso2nfs
{
    public class PackConfiguration
    {
        public bool IsEncrypted { get; set; }
        public bool KeepFiles { get; set; }
        public string KeyFilePath { get; set; } = Path.Combine("..", "code", "htk.bin");
        public string IsoFilePath { get; set; } = "game.iso";
        public string WiiKeyFilePath { get; set; } = "wii_common_key.bin";
        public string NfsDirectory { get; set; } = string.Empty;
        public string FwImageFilePath { get; set; } = string.Empty;
        public bool KeepLegit { get; set; } = false;
        public bool MapShoulderToTrigger { get; set; } = false;
        public bool VerticalWiimote { get; set; } = false;
        public bool HorizontalWiimote { get; set; } = false;
        public bool HomebrewPatches { get; set; } = false;
        public bool PassthroughMode { get; set; } = false;
        public bool InstantCC { get; set; } = false;
        public bool NoClassicController { get; set; } = false;
        public string OutputDirectory { get; set; } = Directory.GetCurrentDirectory();
    }
}
