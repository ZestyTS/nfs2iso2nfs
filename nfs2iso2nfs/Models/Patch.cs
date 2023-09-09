using nfs2iso2nfs.Helpers;
using nfs2iso2nfs.Models;
namespace nfs2iso2nfs.Models
{
    public class Patch
    {
        public bool KeepLegit { get; set; }
        public bool MapShoulderToTrigger { get; set; }
        public bool HorizWiimote { get; set; }
        public bool VertWiimote { get; set; }
        public bool Homebrew { get; set; }
        public bool PassThrough { get; set; }
        public bool InstantCC { get; set; }
        public bool NoCC { get; set; }
        public string FwFile { get; set; }
        public Patch(string fwFile)
        {
            FwFile = fwFile;
        }
        public async Task DoThePatchingAsync()
        {
            // Asynchronously read file into a memory stream
            using var inputMemoryStream = new MemoryStream();
            using (var fileStream = new FileStream(FwFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
                await fileStream.CopyToAsync(inputMemoryStream); //copy fw.img into a memory stream

            // Now, create an AsyncMemoryStream from the MemoryStream
            using var inputIos = new AsyncMemoryStream(inputMemoryStream);

            PatcherHelper.CheckIfRev509(inputIos);

            if (!KeepLegit)
                await PatcherHelper.DontKeepLegitAsync(inputIos);
            if (MapShoulderToTrigger)
                await PatcherHelper.MapShoulderToTriggerAsync(inputIos);
            if (HorizWiimote || VertWiimote)
                await PatcherHelper.EnableWiiRemoteEmulationAsync(inputIos);
            if (HorizWiimote)
                await PatcherHelper.EnableHorizontalWiiRemoteEmulationAsync(inputIos);
            if (Homebrew)
                await PatcherHelper.EnableProperInputInHomebrewAsync(inputIos);
            if (PassThrough)
                await PatcherHelper.WiiMotePassthroughAsync(inputIos);
            if (InstantCC)
                await PatcherHelper.InstantCCAsync(inputIos);
            if (NoCC)
                await PatcherHelper.NoCCAsync(inputIos);

            // Asynchronously write the patched memory stream back to file
            using var patchedFile = new FileStream(FwFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            inputIos.Position = 0;  // Reset the position before writing
            await inputIos.CopyToAsync(patchedFile);
        }
    }
}
