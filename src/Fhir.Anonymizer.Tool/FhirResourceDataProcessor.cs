using System;
using System.IO;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.Tool
{
    public class FhirResourceDataProcessor
    {
        private readonly string _configFilePath;

        public FhirResourceDataProcessor(string configFilePath)
        {
            _configFilePath = configFilePath;
        }

        public async Task AnonymizeFolder(string inputFolder, string outputFolder, bool isRecursive, bool validateInput, bool validateOutput)
        {
            var anonymizer = new FilesAnonymizerForJsonFormatResource(_configFilePath, inputFolder, outputFolder, isRecursive, validateInput, validateOutput);
            await anonymizer.AnonymizeAsync().ConfigureAwait(false);

            Console.WriteLine($"Finished processing '{inputFolder}'! ");
        }

        public async Task AnonymizeBulkDataFolder(string inputFolder, string outputFolder, bool isRecursive, bool validateInput, bool validateOutput)
        {
            var anonymizer = new FilesAnonymizerForNdJsonFormatResource(_configFilePath, inputFolder, outputFolder, isRecursive, validateInput, validateOutput);
            await anonymizer.AnonymizeAsync().ConfigureAwait(false);

            Console.WriteLine($"Finished processing '{inputFolder}'!");
        }

        public bool IsSameDirectory(string inputFolder, string outputFolder)
        {
            string inputFolderPath = Path.GetFullPath(inputFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string outputFolderPath = Path.GetFullPath(outputFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return string.Equals(inputFolderPath, outputFolderPath, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
