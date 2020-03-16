using System;
using System.IO;
using System.Linq;
using Fhir.Anonymizer.Core;

namespace Fhir.Anonymizer.Tool
{
    public class FhirResourceDataProcessor
    {
        private readonly AnonymizerEngine _engine;

        public FhirResourceDataProcessor(string configFilePath)
        {
            _engine = new AnonymizerEngine(configFilePath);
        }

        public void AnonymizeFolder(string inputFolder, string outputFolder, bool isRecursive)
        {
            var directorySearchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var resourceFileList = Directory.EnumerateFiles(inputFolder, "*.json", directorySearchOption).ToList();
            Console.WriteLine($"Find {resourceFileList.Count()} json resource files in '{inputFolder}'.");

            var processedCount = 0;
            var processedErrorCount = 0;

            foreach (var resourceFileName in resourceFileList)
            {
                Console.WriteLine($"Processing {resourceFileName}");

                var resourceOutputFileName = GetResourceOutputFileName(resourceFileName, inputFolder, outputFolder);
                if (isRecursive)
                {
                    var resourceOutputFolder = Path.GetDirectoryName(resourceOutputFileName);
                    Directory.CreateDirectory(resourceOutputFolder);
                }

                using (FileStream inputStream = new FileStream(resourceFileName, FileMode.Open))
                using (FileStream outputStream = new FileStream(resourceOutputFileName, FileMode.Create))
                {
                    using StreamReader reader = new StreamReader(inputStream);
                    using StreamWriter writer = new StreamWriter(outputStream);
                    var resourceJson = reader.ReadToEnd();
                    try
                    {
                        var resourceResult = _engine.AnonymizeJson(resourceJson, isPrettyOutput: true);
                        writer.Write(resourceResult);
                        processedCount += 1;
                    }
                    catch (Exception innerException)
                    {
                        processedErrorCount += 1;
                        Console.Error.WriteLine($"Error #{processedErrorCount}\nResource: {resourceJson}\nErrorMessage: {innerException.ToString()}");
                    }
                }
            }

            Console.WriteLine($"Finished processing '{inputFolder}'! Succeeded in {processedCount} resources, failed in {processedErrorCount} resources in total.");
        }

        public void AnonymizeBulkDataFolder(string inputFolder, string outputFolder, bool isRecursive)
        {
            var directorySearchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var bulkResourceFileList = Directory.EnumerateFiles(inputFolder, "*.ndjson", directorySearchOption).ToList();
            Console.WriteLine($"Find {bulkResourceFileList.Count()} bulk data resource files in '{inputFolder}'.");

            foreach (var bulkResourceFileName in bulkResourceFileList)
            {
                Console.WriteLine($"Processing {bulkResourceFileName}");

                var bulkResourceOutputFileName = GetResourceOutputFileName(bulkResourceFileName, inputFolder, outputFolder);
                if (isRecursive)
                {
                    var resourceOutputFolder = Path.GetDirectoryName(bulkResourceOutputFileName);
                    Directory.CreateDirectory(resourceOutputFolder);
                }

                var processedCount = 0;
                var processedErrorCount = 0;
                using (FileStream inputStream = new FileStream(bulkResourceFileName, FileMode.Open))
                using (FileStream outputStream = new FileStream(bulkResourceOutputFileName, FileMode.Create))
                {
                    using StreamReader reader = new StreamReader(inputStream);
                    using StreamWriter writer = new StreamWriter(outputStream);

                    string resourceLine;
                    string resultLine;
                    while ((resourceLine = reader.ReadLine()) != null)
                    {
                        try
                        {
                            resultLine = _engine.AnonymizeJson(resourceLine);
                            writer.WriteLine(resultLine);
                            processedCount += 1;
                        }
                        catch (Exception innerException)
                        {
                            processedErrorCount += 1;
                            Console.Error.WriteLine($"Error #{processedErrorCount}\nResource: {resourceLine}\nErrorMessage: {innerException.ToString()}");
                        }
                    }
                }
                
                Console.WriteLine($"Finished processing '{bulkResourceFileName}'! Succeeded in {processedCount} resources, failed in {processedErrorCount} resources in total.");
            }
        }

        public bool IsSameDirectory(string inputFolder, string outputFolder)
        {
            string inputFolderPath = Path.GetFullPath(inputFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string outputFolderPath = Path.GetFullPath(outputFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return string.Equals(inputFolderPath, outputFolderPath, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResourceOutputFileName(string fileName, string inputFolder, string outputFolder)
        {
            var partialFilename = fileName.Substring(inputFolder.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return Path.Combine(outputFolder, partialFilename);
        }
    }
}
