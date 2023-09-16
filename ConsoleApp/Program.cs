namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var htk = "";
            var output = "";
            var fwImg = "";
            var iso = "";

            string[] dargs = {"-enc", "-homebrew", "-iso", iso, "-fwimg", fwImg, "-key", htk, "-output", output};

            var packInstance = new nfs2iso2nfs.Pack();

            packInstance.Convert(dargs).GetAwaiter().GetResult();
        }
    }
}