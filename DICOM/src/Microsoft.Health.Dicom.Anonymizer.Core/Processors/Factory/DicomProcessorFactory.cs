// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public class DicomProcessorFactory : IAnonymizerProcessorFactory
    {
        public IAnonymizerProcessor CreateProcessor(string method, JObject settingObject = null, IAnonymizerSettingsFactory settingsFactory = null)
        {
            return method.ToLower() switch
            {
                "perturb" => new PerturbProcessor(settingObject, settingsFactory),
                "substitute" => new SubstituteProcessor(settingObject),
                "dateshift" => new DateShiftProcessor(settingObject, settingsFactory),
                "encrypt" => new EncryptionProcessor(settingObject, settingsFactory),
                "cryptohash" => new CryptoHashProcessor(settingObject, settingsFactory),
                "redact" => new RedactProcessor(settingObject, settingsFactory),
                "remove" => new RemoveProcessor(),
                "refreshuid" => new RefreshUIDProcessor(),
                "keep" => new KeepProcessor(),
                _ => null,
            };
        }
    }
}
