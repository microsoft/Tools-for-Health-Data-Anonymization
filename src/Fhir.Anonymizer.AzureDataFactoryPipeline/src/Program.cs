using System;
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
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
               .WithParsed<Options>(option => new DataFactoryCustomActivity().Run(option.Force));
        }
    }
}
