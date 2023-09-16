# nfs2iso2nfs
Convert nfs files to iso and back

## This Branch
* Updated to support .Net 7
* Written as a Class Library instead of a Console App with a focus of moving over to Objects and Helpers
* All the code is moved over to be Async
* Added support to run it as a Console App with similar performance, there will be different information written out to the console
* Added simple Unit Tests for the Helper functions
* Added a logger
  * "ILogger<Pack> logger"

### Example
* No Logging
  * string[] args = {"-enc", "-homebrew", "-iso", iso, "-fwimg", fwImg, "-key", htk, "-output", output};
  * var packInstance = new nfs2iso2nfs.Pack();
  * await packInstance.Convert(aargs);

* Logging
  * string[] args = {"-enc", "-homebrew", "-iso", iso, "-fwimg", fwImg, "-key", htk, "-output", output};
  * var packInstance = new nfs2iso2nfs.Pack(_logger);
  * await packInstance.Convert(aargs);
