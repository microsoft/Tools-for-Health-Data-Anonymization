// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Anonymizer.Common
{
    internal static class Constants
    {
        internal const string YearFormat = "yyyy";

        // Refer to HIPPA standard https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html
        internal const int AgeThreshold = 89;
    }
}
