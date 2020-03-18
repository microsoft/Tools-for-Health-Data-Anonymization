using System;
using System.Threading.Tasks;
using CommandLine;

namespace Fhir.Anonymizer.DataFactoryTool
{
    public class Options
    {
        [Option('f', "force", Required = false, HelpText = "Force overwrite the exist blob files in the output container.")]
        public bool Force { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
               .MapResult(async option => await new DataFactoryCustomActivity().Run(option.Force).ConfigureAwait(false), _ => Task.FromResult(1)).ConfigureAwait(false);
        }
    }
}
