// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
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
                    new AnonymizerSettings(options.ValidateInput, options.ValidateOutput));
            if (options.InputFile != null && options.OutputFile != null)
            {
                await AnonymizeOneFileAsync(options.InputFile, options.OutputFile, engine, options.SkipFailedItem);
            }
            else if (options.InputFolder != null && options.OutputFolder != null)
            {
                if (IsSameDirectory(options.InputFolder, options.OutputFolder))
                {
                    Console.Error.WriteLine("Input and output folders are the same! Please choose another folder.");
                }

                Directory.CreateDirectory(options.OutputFolder);

                foreach (string file in Directory.EnumerateFiles(options.InputFolder, "*.dcm", SearchOption.AllDirectories))
                {
                    await AnonymizeOneFileAsync(file, Path.Join(options.OutputFolder, Path.GetFileName(file)), engine, options.SkipFailedItem);
                }

                Console.WriteLine("Anonymization finished!");
            }
            else
            {
                Console.Error.WriteLine($"Invalid command line. Please specify inputFile( or inputFolder) and outputFile( or outputFolder) at the same time.");
            }
        }

        private static bool IsSameDirectory(string inputFolder, string outputFolder)
        {
            string inputFolderPath = Path.GetFullPath(inputFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string outputFolderPath = Path.GetFullPath(outputFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return string.Equals(inputFolderPath, outputFolderPath, StringComparison.InvariantCultureIgnoreCase);
        }

        internal static async Task AnonymizeOneFileAsync(string inputFile, string outputFile, AnonymizerEngine engine, bool skipFailedItem)
        {
            DicomFile dicomFile = await DicomFile.OpenAsync(inputFile).ConfigureAwait(false);
            try
            {
                engine.AnonymizeDataset(dicomFile.Dataset);
                dicomFile.Save(outputFile);
            }
            catch
            {
                if (skipFailedItem)
                {
                    Console.WriteLine($"Fail and skip the file '{inputFile}'!");
                }
                else
                {
                    throw;
                }
            }

            Console.WriteLine($"Finished processing '{inputFile}'!");
        }
    }
}