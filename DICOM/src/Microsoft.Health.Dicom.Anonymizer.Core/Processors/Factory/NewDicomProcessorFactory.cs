// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors.Factory
{
    public class NewDicomProcessorFactory
    {
        private Dictionary<JObject, IAnonymizerProcessor> _processors;
        /*
        public IAnonymizerProcessor GetProcessor(string method, JObject anongmizerSettings)
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
        */
    }
}
