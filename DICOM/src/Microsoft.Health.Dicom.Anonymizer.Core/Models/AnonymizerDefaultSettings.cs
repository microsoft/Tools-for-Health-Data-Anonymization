// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Models
{
    [DataContract]
    public class AnonymizerDefaultSettings
    {
        [DataMember(Name = "perturb")]
        public PerturbSettings PerturbDefaultSetting { get; set; }

        [DataMember(Name = "substitute")]
        public SubstituteSettings SubstituteDefaultSetting { get; set; }

        [DataMember(Name = "dateshift")]
        public DateShiftSettings DateShiftDefaultSetting { get; set; }

        [DataMember(Name = "encrypt")]
        public EncryptSettings EncryptDefaultSetting { get; set; }

        [DataMember(Name = "cryptoHash")]
        public CryptoHashSettings CryptoHashDefaultSetting { get; set; }

        [DataMember(Name = "redact")]
        public RedactSettings RedactDefaultSetting { get; set; }

        public Dictionary<string, object> GetDefaultSetting(string method)
        {
            return method.ToLower() switch
            {
                "perturb" => ConvertToDict(PerturbDefaultSetting),
                "substitute" => ConvertToDict(SubstituteDefaultSetting),
                "dateshift" => ConvertToDict(DateShiftDefaultSetting),
                "encrypt" => ConvertToDict(EncryptDefaultSetting),
                "cryptohash" => ConvertToDict(CryptoHashDefaultSetting),
                "redact" => ConvertToDict(RedactDefaultSetting),
                _ => null,
            };
        }

        private static Dictionary<string, object> ConvertToDict<T>(T settings)
        {
            if (settings == null) return null;
            
            var json = JsonConvert.SerializeObject(settings);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }
    }
}
