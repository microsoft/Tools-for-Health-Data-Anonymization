// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class DicomProcessorFactory : IAnonymizerProcessorFactory
    {
        public virtual IAnonymizerProcessor CreateProcessor(string method, JObject settingObject = null)
        {
            EnsureArg.IsNotNullOrEmpty(method, nameof(method));

            return method.ToLower() switch
            {
                "perturb" => new PerturbProcessor(settingObject),
                "substitute" => new SubstituteProcessor(settingObject),
                "dateshift" => new DateShiftProcessor(settingObject),
                "encrypt" => new EncryptProcessor(settingObject),
                "cryptohash" => new CryptoHashProcessor(settingObject),
                "redact" => new RedactProcessor(settingObject),
                "remove" => new RemoveProcessor(),
                "refreshuid" => new RefreshUIDProcessor(),
                "keep" => new KeepProcessor(),
                _ => null,
            };
        }
    }
}
