// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public interface IAnonymizerProcessorFactory
    {
        public IAnonymizerProcessor CreateProcessor(string method, JObject ruleSetting = null, IAnonymizerSettingsFactory settingsFactory = null);
    }
}
