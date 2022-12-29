// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Fhir.Models;
using System.Threading.Channels;
using Microsoft.Health.DeIdentification.Batch.Model;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class LocalFhirDataLoader : DataLoader<ResourceList>
    {
        public BatchFhirDeIdJobInputData inputData { get; set; }
        protected override async Task LoadDataInternalAsync(Channel<ResourceList> outputChannel, CancellationToken cancellationToken)
        {
            string _outputFolder = inputData.DestinationDataset.URL;
            string _inputFolder;
            List<string> files = GetFiles(inputData.SourceDataset.URL, inputData.SourceDataset.DataFormatType.ToString().ToLower(), out _inputFolder);

            foreach (var fileName in files)
            {
                Console.WriteLine($"Processing {fileName}");

                var outputFileName = GetResourceOutputFileName(fileName, _inputFolder, _outputFolder);

                var resourceOutputFolder = Path.GetDirectoryName(outputFileName);
                Directory.CreateDirectory(resourceOutputFolder);

                // TODO: add config to skip existing file?
                if (File.Exists(outputFileName))
                {
                    Console.WriteLine($"Remove existed target file {outputFileName}.");
                    File.Delete(outputFileName);
                }

                List<string> initialData = new List<string>();
                switch (inputData.SourceDataset.DataFormatType)
                {
                    case DataFormatType.Ndjson:
                        using (StreamReader reader = new StreamReader(fileName))
                        {
                            while (!reader.EndOfStream)
                            {
                                initialData.Add(reader.ReadLine());
                            }
                        }

                        break;
                    case DataFormatType.Json:
                        string resourceJson = await File.ReadAllTextAsync(fileName).ConfigureAwait(false);
                        initialData.Add(resourceJson);
                        break;
                    default:
                        throw new NotSupportedException($"The data format type {inputData.SourceDataset.DataFormatType} is unsupported by FHIR.");
                }

                await outputChannel.Writer.WriteAsync(new ResourceList() { Resources = initialData, inputFileName = fileName, outputFileName = outputFileName }, cancellationToken);

                Console.WriteLine($"Finished processing '{fileName}'!");
            }

            outputChannel.Writer.Complete();
        }

        private string GetResourceOutputFileName(string fileName, string inputFolder, string outputFolder)
        {
            var partialFilename = fileName.Substring(inputFolder.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return Path.Combine(outputFolder, partialFilename);
        }

        private static List<string> GetFiles(string url, string dataFormatType, out string inputFolder)
        {
            List<string> fileList;
            if (File.Exists(url))
            {
                fileList = new List<string> { url };
                inputFolder = Path.GetDirectoryName(url);
            }
            else if (Directory.Exists(url))
            {
                inputFolder = url; 
                fileList = Directory.EnumerateFiles(inputFolder, $"*.{dataFormatType}", SearchOption.AllDirectories).ToList();

                Console.WriteLine($"Find {fileList.Count()} data resource files in '{inputFolder}'.");
            }
            else
            {
                string patten = url.Split('\\').Last();
                inputFolder = Path.GetDirectoryName(url);

                fileList = Directory.EnumerateFiles(inputFolder, patten, SearchOption.AllDirectories).Where(f => f.EndsWith($".{dataFormatType}")).ToList();

                Console.WriteLine($"Find {fileList.Count()} data resource files in '{inputFolder}'.");
            }

            return fileList;
        }
    }
}
