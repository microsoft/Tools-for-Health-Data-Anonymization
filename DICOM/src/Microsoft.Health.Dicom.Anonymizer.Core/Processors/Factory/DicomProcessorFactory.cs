// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public class DicomProcessorFactory : IAnonymizerProcessorFactory
    {
        public IAnonymizerProcessor CreateProcessor(string method, IDicomAnonymizationSetting ruleSetting = null)
        {
            return method.ToLower() switch
            {
                "perturb" => new PerturbProcessor(ruleSetting),
                "substitute" => new SubstituteProcessor(ruleSetting),
                "dateshift" => new DateShiftProcessor(ruleSetting),
                "encrypt" => new EncryptionProcessor(ruleSetting),
                "cryptohash" => new CryptoHashProcessor(ruleSetting),
                "redact" => new RedactProcessor(ruleSetting),
                "remove" => new RemoveProcessor(),
                "refreshuid" => new RefreshUIDProcessor(),
                "keep" => new KeepProcessor(),
                _ => null,
            };
        }
    }
}
