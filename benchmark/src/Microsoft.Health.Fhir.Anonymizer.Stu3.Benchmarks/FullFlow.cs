using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.Health.Fhir.Anonymizer.Tool;
using System.Collections.Generic;
using System.IO;
using System;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Anonymizer.Benchmarks
{
    [MemoryDiagnoser]
    [CsvMeasurementsExporter]
    public class FullFlow
    {
        public static readonly string RootPath = Environment.GetEnvironmentVariable("FHIRCLIBENCHMARKROOT");
        private readonly string inputFolder =  Path.Combine(RootPath,"input");
        private readonly string outputFolder = Path.Combine(RootPath,"output");
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
        public string FullPath => Path.Combine(FullFlow.RootPath,"config",FileName);

        public override string ToString() => Name;
    }
}
