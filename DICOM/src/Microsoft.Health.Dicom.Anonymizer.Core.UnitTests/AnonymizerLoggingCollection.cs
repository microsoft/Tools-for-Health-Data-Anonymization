// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    // Other collections also create loggers and must not observe a temporary global factory being disposed.
    [CollectionDefinition("Anonymizer logging state", DisableParallelization = true)]
    public class AnonymizerLoggingCollection
    {
    }
}
