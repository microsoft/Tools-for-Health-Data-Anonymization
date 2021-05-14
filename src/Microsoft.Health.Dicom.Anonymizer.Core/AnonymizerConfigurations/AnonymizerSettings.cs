// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations
{
    public class AnonymizerSettings
    {
        public bool ValidateInput { get; set; } = false;

        public bool SkipFailedItem { get; set; } = true;

        public bool AutoValidate { get; set; } = true;
    }
}
