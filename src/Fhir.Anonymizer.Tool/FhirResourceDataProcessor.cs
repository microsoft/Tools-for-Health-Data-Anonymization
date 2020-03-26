﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.PartitionedExecution;

namespace Fhir.Anonymizer.Tool
{
    public class FhirResourceDataProcessor
    {
        private readonly AnonymizerEngine _engine;

        public FhirResourceDataProcessor(string configFilePath)
        {
            _engine = new AnonymizerEngine(configFilePath);
        }

        public async Task AnonymizeFolder(string inputFolder, string outputFolder, bool isRecursive)
        {
            var anonymizer = new FilesAnonymizerForJsonFormatResource(_engine, inputFolder, outputFolder, isRecursive);
            await anonymizer.AnonymizeAsync().ConfigureAwait(false);
            
            Console.WriteLine($"Finished processing '{inputFolder}'! ");
        }

        public async Task AnonymizeBulkDataFolder(string inputFolder, string outputFolder, bool isRecursive)
        {
            var anonymizer = new FilesAnonymizerForNdJsonFormatResource(_engine, inputFolder, outputFolder, isRecursive);
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
