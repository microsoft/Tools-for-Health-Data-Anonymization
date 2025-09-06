// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Anonymizer.CommandLineTool
{
    public class AnonymizerOptions
    {
        public string InputFile { get; set; }

        public string OutputFile { get; set; }

        public string ConfigurationFilePath { get; set; }

        public string InputFolder { get; set; }

        public string OutputFolder { get; set; }

        public bool ValidateInput { get; set; }

        public bool ValidateOutput { get; set; }
    }
}
