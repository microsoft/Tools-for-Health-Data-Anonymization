// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using CommandLine;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Tool
{
    public class AnonymizerOptions
    {
        [Option('i', "inputFile", Required = false, HelpText = "Input dicom file")]
        public string InputFile { get; set; }

        [Option('o', "outputFile", Required = false, HelpText = "Output dicom file")]
        public string OutputFile { get; set; }

        [Option('c', "configFile", Required = false, Default = "configuration.json", HelpText = "Anonymization configuration file path.")]
        public string ConfigurationFilePath { get; set; }

        [Option('I', "inputFolder", Required = false, HelpText = "Input folder")]
        public string InputFolder { get; set; }

        [Option('O', "outputFolder", Required = false, HelpText = "Output folder")]
        public string OutputFolder { get; set; }

        [Option('v', "autoValidate", Required = false, Default = true, HelpText = "Auto validate output value when anonymizing.")]
        public bool AutoValidate { get; set; }

        [Option('s', "skipFailedItem", Required = false, Default = true, HelpText = "Skip failed item when anonymizing and maintain the original value.")]
        public bool SkipFailedItem { get; set; }

        [Option("validateInput", Required = false, Default = false, HelpText = "Validate input dicom data items.")]
        public bool ValidateInput { get; set; }
    }
}