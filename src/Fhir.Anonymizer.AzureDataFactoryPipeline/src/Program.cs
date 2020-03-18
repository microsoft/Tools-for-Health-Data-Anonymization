using System;
using System.Threading.Tasks;
using CommandLine;

namespace Fhir.Anonymizer.DataFactoryTool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new DataFactoryCustomActivity().Run().ConfigureAwait(false);
        }
    }
}
