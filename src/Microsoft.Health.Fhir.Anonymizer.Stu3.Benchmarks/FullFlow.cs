using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Tool;
using System;
using System.Collections.Generic;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Anonymizer.Benchmarks
{
    //these are better set via the command line for running the benchmarks:
    //[ShortRunJob(RuntimeMoniker.NetCoreApp31)]
    //[ShortRunJob(RuntimeMoniker.NetCoreApp50)]
    //[MemoryDiagnoser]
    [CsvMeasurementsExporter]
    [RPlotExporter]
    public class FullFlow
    {
        private readonly string inputFolder = @"C:\fhircli\benchmarks\samples\input";
        private readonly string outputFolder = @"C:\fhircli\benchmarks\samples\output";
        private readonly AnonymizationToolOptions toolOptions = new AnonymizationToolOptions { IsRecursive = true };

        public IEnumerable<BenchmarkConfig> Configs()
        {
            yield return new BenchmarkConfig("Default", "configuration-sample.json");
            yield return new BenchmarkConfig("Presidio", "configuration-sample-presidio.json");
        }        

        [Benchmark]
        [ArgumentsSource(nameof(Configs))]
        public async Task AnonymizeJsonFiles(BenchmarkConfig config)
        {
            await new FilesAnonymizerForJsonFormatResource(config.FullPath, inputFolder, outputFolder, toolOptions)
                                .AnonymizeAsync()
                                .ConfigureAwait(false);
        }
    }

    public class BenchmarkConfig
    {
        public BenchmarkConfig(string name, string fileName)
        {
            Name = name;
            FileName = fileName;
        }

        public string Name { get; set; }
        public string FileName { get; set; }
        public string FullPath => @"C:\fhircli\benchmarks\samples\config\" + FileName;

        public override string ToString() => Name;
    }
}
