// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class DicomProcessorFactory : IAnonymizerProcessorFactory
    {
        private readonly Dictionary<string, IAnonymizerProcessor> _customProcessors = new Dictionary<string, IAnonymizerProcessor>() { };

        public IAnonymizerProcessor CreateProcessor(string method, JObject settingObject = null)
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
                _ => _customProcessors.GetValueOrDefault(method)
            };
        }

        public void AddCustomProcessor(string method, IAnonymizerProcessor processor)
        {
            EnsureArg.IsNotNullOrEmpty(method, nameof(method));
            EnsureArg.IsNotNull(processor, nameof(processor));

            _customProcessors[method.ToLower()] = processor;
        }
    }
}
