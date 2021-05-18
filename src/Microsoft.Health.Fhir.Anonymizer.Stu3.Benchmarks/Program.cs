using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core;
using System;

namespace Microsoft.Health.Fhir.Anonymizer.Benchmarks
{
    public class Program
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<Program>();

        static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);


        [GlobalSetup]
        public void GlobalSetup()
        {
            Console.WriteLine("// GlobalSetup");
            AnonymizerLogging.LoggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddFilter("Microsoft", LogLevel.Warning)
                       .AddFilter("System", LogLevel.Warning)
                       .AddFilter("Fhir.Anonymizer", LogLevel.Debug)
                       .AddConsole();
            });

            _logger.LogDebug("// GlobalSetup from logger");
        }
    }
}
