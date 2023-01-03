// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Fhir.Models;
using System.Threading.Channels;
using Microsoft.Health.DeIdentification.Batch.Model;
using Microsoft.Health.DeIdentification.Fhir.Model;
using Microsoft.Health.DeIdentification.Batch.Models.Data;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class LocalFhirDataLoader : DataLoader<BatchFhirDataContext>
    {
        private readonly ILogger<LocalFhirDataLoader> _logger;

        public BatchFhirDeIdJobInputData inputData { get; set; }

        public LocalFhirDataLoader(ILogger<LocalFhirDataLoader> logger)
        {
            _logger = logger;
        }

        protected override async Task LoadDataInternalAsync(Channel<BatchFhirDataContext> outputChannel, CancellationToken cancellationToken)
        {
            string outputFolder = inputData.DestinationDataset.URL;
            string inputFolder;
            List<string> files = GetFiles(inputData.SourceDataset.URL, inputData.SourceDataset.DataFormatType.ToString().ToLower(), out inputFolder);

            foreach (var fileName in files)
            {
                _logger.LogInformation($"Processing {fileName}");

                var outputFileName = GetResourceOutputFileName(fileName, inputFolder, outputFolder);

                var resourceOutputFolder = Path.GetDirectoryName(outputFileName);
                Directory.CreateDirectory(resourceOutputFolder);

                // TODO: add config to skip existing file?
                if (File.Exists(outputFileName))
                {
                    _logger.LogInformation($"Remove existed target file {outputFileName}.");
                    File.Delete(outputFileName);
                }

                var initialData = new StringBatchData();
                switch (inputData.SourceDataset.DataFormatType)
                {
                    case DataFormatType.Ndjson:
                        using (StreamReader reader = new StreamReader(fileName))
                        {
                            while (!reader.EndOfStream)
                            {
                                initialData.Resources.Add(reader.ReadLine());
                            }
                        }

                        break;
                    case DataFormatType.Json:
                        string resourceJson = await File.ReadAllTextAsync(fileName).ConfigureAwait(false);
                        initialData.Resources.Add(resourceJson);
                        break;
                    default:
                        throw new NotSupportedException($"The data format type {inputData.SourceDataset.DataFormatType} is unsupported by FHIR.");
                }

                await outputChannel.Writer.WriteAsync(new BatchFhirDataContext() { Resources = initialData, InputFileName = fileName, OutputFileName = outputFileName}, cancellationToken);

                _logger.LogInformation($"Finished processing '{fileName}'!");
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
                string patten = Path.GetFileName(url);
                inputFolder = Path.GetDirectoryName(url);

                fileList = Directory.EnumerateFiles(inputFolder, patten, SearchOption.AllDirectories).Where(f => f.EndsWith($".{dataFormatType}")).ToList();

                Console.WriteLine($"Find {fileList.Count()} data resource files in '{inputFolder}'.");
            }

            return fileList;
        }
    }
}
