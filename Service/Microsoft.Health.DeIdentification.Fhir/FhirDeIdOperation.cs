using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.PartitionedExecution;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdOperation : IDeIdOperation<List<Object>, string>
    {
        private AnonymizerEngine _anonymizerEngine;
        private string _ndjsonPath;
        private string _configPath;

        public FhirDeIdOperation(string configPath)
        {
            _configPath = configPath;
            AnonymizerEngine.InitializeFhirPathExtensionSymbols();
            _anonymizerEngine = new AnonymizerEngine(configPath);
        }

        public FhirDeIdOperation(string configPath, string ndjsonPath)
        {
            AnonymizerEngine.InitializeFhirPathExtensionSymbols();
            _ndjsonPath = ndjsonPath;
            _configPath = configPath;
        }

        public string Process(List<Object> source)
        {
            var result = new StringBuilder();
            foreach (var item in source)
            {
                result.Append(ProcessSingle(item.ToString()));
            }
            return result.ToString();
        }

        public string ProcessSingle(string context)
        {
            return _anonymizerEngine.AnonymizeJson(context);
        }

        public async Task ProcessNdjson()
        {
            int completedCount = 0;
            int skippedCount = 0;
            int consumeCompletedCount = 0;
            string outputPath = "D:\\files\\outputs\\test.ndjson";
            using (FileStream inputStream = new FileStream(_ndjsonPath, FileMode.Open))
            using (FileStream outputStream = new FileStream(outputPath, FileMode.Create))
            {
                using FhirStreamReader reader = new FhirStreamReader(inputStream);
                using FhirStreamConsumer consumer = new FhirStreamConsumer(outputStream);
                var engine = AnonymizerEngine.CreateWithFileContext(_configPath, _ndjsonPath, "D:\\files\\inputs");
                Func<string, string> anonymizeFunction = (content) =>
                {
                    try
                    {
                        var settings = new AnonymizerSettings()
                        {
                            IsPrettyOutput = false,
                            ValidateInput = false,
                            ValidateOutput = false
                        };
                        return engine.AnonymizeJson(content, settings);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"ErrorMessage: {ex}");
                        throw;
                    }
                };
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                FhirPartitionedExecutor<string, string> executor = new FhirPartitionedExecutor<string, string>(reader, consumer, anonymizeFunction);
                executor.PartitionCount = Environment.ProcessorCount * 2;

                Progress<BatchAnonymizeProgressDetail> progress = new Progress<BatchAnonymizeProgressDetail>();
                progress.ProgressChanged += (obj, args) =>
                {
                    Interlocked.Add(ref completedCount, args.ProcessCompleted);
                    Interlocked.Add(ref skippedCount, args.ProcessSkipped);
                    Interlocked.Add(ref consumeCompletedCount, args.ConsumeCompleted);

                    Console.WriteLine($"[{stopWatch.Elapsed.ToString()}][tid:{args.CurrentThreadId}]: {completedCount} Process completed. {skippedCount} Process skipped. {consumeCompletedCount} Consume completed.");
                };

                await executor.ExecuteAsync(CancellationToken.None, progress).ConfigureAwait(false);
            }

            Console.WriteLine($"Finished processing '{_ndjsonPath}'!");
        }
    }
}