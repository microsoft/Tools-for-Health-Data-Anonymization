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
            try
            {
                var engine = new AnonymizerEngine(
                    options.ConfigurationFilePath,
                    new AnonymizerSettings()
                    {
                        SkipFailedItem = options.SkipFailedItem,
                        AutoValidate = options.AutoValidate,
                        ValidateInput = options.ValidateInput,
                    });
                if (options.InputFile != null && options.OutputFile != null)
                {
                    await AnonymizeOneFile(options.InputFile, options.OutputFile, engine);
                }
                else if (options.InputFolder != null && options.OutputFolder != null)
                {
                    if (IsSameDirectory(options.InputFolder, options.OutputFolder))
                    {
                        throw new Exception("Input and output folders are the same! Please choose another folder.");
                    }

                    Directory.CreateDirectory(options.OutputFolder);

                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    var num = 0;
                    foreach (string file in Directory.EnumerateFiles(options.InputFolder, "*.dcm", SearchOption.AllDirectories))
                    {
                        Console.WriteLine(file);
                        await AnonymizeOneFile(file, Path.Join(options.OutputFolder, Path.GetFileName(file)), engine);
                        num++;
                    }

                    sw.Stop();
                    TimeSpan ts = sw.Elapsed;
                    Console.WriteLine("{1} items.DateTime costed for Shuffle function is: {0}ms", ts.TotalMilliseconds, num);
                }
                else
                {
                    Console.WriteLine($"Invalid command line. Please specify inputFile( or inputFolder) and outputFile( or outputFolder) at the same time.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process failed! {ex}");
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

        internal static async Task AnonymizeOneFile(string inputFile, string outputFile, AnonymizerEngine engine)
        {
            DicomFile dicomFile = await DicomFile.OpenAsync(inputFile).ConfigureAwait(false);

            // Console.WriteLine("{0,-15}{1,-40}{2,-15}{3,-50}{4,-70}", "Tag", "Name", "De-ID Method", "Original Value", "Result");
            // Console.WriteLine(new string('-', 150));
            engine.Anonymize(dicomFile.Dataset);
            dicomFile.Save(outputFile);

            // Console.WriteLine($"Finished processing '{inputFile}'!");
        }
    }
}