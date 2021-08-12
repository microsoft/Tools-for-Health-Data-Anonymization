// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using CommandLine;

namespace Microsoft.Health.Dicom.Anonymizer.CommandLineTool
{
    public class AnonymizerOptions
    {
        [Option('i', "inputFile", Required = false, HelpText = "Input DICOM file")]
        public string InputFile { get; set; }

        [Option('o', "outputFile", Required = false, HelpText = "Output DICOM file")]
        public string OutputFile { get; set; }

        [Option('c', "configFile", Required = false, Default = "configuration.json", HelpText = "Anonymization configuration file path.")]
        public string ConfigurationFilePath { get; set; }

        [Option('I', "inputFolder", Required = false, HelpText = "Input folder")]
        public string InputFolder { get; set; }

        [Option('O', "outputFolder", Required = false, HelpText = "Output folder")]
        public string OutputFolder { get; set; }

        [Option("validateInput", Required = false, Default = false, HelpText = "Validate input DICOM data items.")]
        public bool ValidateInput { get; set; }

        [Option("validateOutput", Required = false, Default = false, HelpText = "Validate output DICOM data items.")]
        public bool ValidateOutput { get; set; }
    }
}
