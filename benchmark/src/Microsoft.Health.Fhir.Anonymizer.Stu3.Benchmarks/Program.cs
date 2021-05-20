using BenchmarkDotNet.Running;

namespace Microsoft.Health.Fhir.Anonymizer.Benchmarks
{
    public class Program
    {
        static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
