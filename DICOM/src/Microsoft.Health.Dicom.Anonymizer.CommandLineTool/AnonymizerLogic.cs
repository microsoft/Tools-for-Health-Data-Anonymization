// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Tool
{
    internal static class AnonymizerLogic
    {
        internal static async Task AnonymizeAsync(AnonymizerOptions options)
        {
            var engine = new AnonymizerEngine(
                    options.ConfigurationFilePath,
                    new AnonymizerEngineOptions(options.ValidateInput, options.ValidateOutput));
            if (options.InputFile != null && options.OutputFile != null)
            {
                if (IsSamePath(options.InputFile, options.OutputFile))
                {
                    Console.Error.WriteLine("Input and output file path are the same! Please check file names.");
                }

                await AnonymizeOneFileAsync(options.InputFile, options.OutputFile, engine, options.SkipFailedItem);
            }
            else if (options.InputFolder != null && options.OutputFolder != null)
            {
                if (IsSamePath(options.InputFolder, options.OutputFolder))
                {
                    Console.Error.WriteLine("Input and output folders are the same! Please choose another folder.");
                }

                foreach (string file in Directory.EnumerateFiles(options.InputFolder, "*.dcm", SearchOption.AllDirectories))
                {
                    await AnonymizeOneFileAsync(file, Path.Join(options.OutputFolder, Path.GetRelativePath(options.InputFolder, file)), engine, options.SkipFailedItem);
                }

                Console.WriteLine("Anonymization finished!");
            }
            else
            {
                Console.Error.WriteLine("Invalid command line. Please specify inputFile( or inputFolder) and outputFile( or outputFolder) at the same time.");
            }
        }

        private static bool IsSamePath(string inputPath, string outputPath)
        {
            string inputFullPath = Path.GetFullPath(inputPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string outputFullPath = Path.GetFullPath(outputPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return string.Equals(inputFullPath, outputFullPath, StringComparison.InvariantCultureIgnoreCase);
        }

        internal static async Task AnonymizeOneFileAsync(string inputFile, string outputFile, AnonymizerEngine engine, bool skipFailedItem)
        {
            try
            {
                DicomFile dicomFile = await DicomFile.OpenAsync(inputFile).ConfigureAwait(false);
                engine.AnonymizeDataset(dicomFile.Dataset);

                if (!string.IsNullOrEmpty(Path.GetDirectoryName(outputFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                }

                await dicomFile.SaveAsync(outputFile);

                Console.WriteLine($"Finished processing '{inputFile}'!");
            }
            catch (Exception ex)
            {
                if (skipFailedItem)
                {
                    Console.WriteLine($"Failed to process the file {inputFile} and skipped. \r\n {ex.Message}");
                }
                else
                {
                    throw;
                }
            }
        }
    }
}