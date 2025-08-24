// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Models
{
    [DataContract]
    public class PerturbSettings
    {
        [DataMember(Name = "span")]
        public double Span { get; set; }

        [DataMember(Name = "roundTo")]
        public int RoundTo { get; set; }

        [DataMember(Name = "rangeType")]
        public string RangeType { get; set; }
    }

    [DataContract]
    public class DateShiftSettings
    {
        [DataMember(Name = "dateShiftKey")]
        public string DateShiftKey { get; set; }

        [DataMember(Name = "dateShiftScope")]
        public string DateShiftScope { get; set; }

        [DataMember(Name = "dateShiftRange")]
        public int DateShiftRange { get; set; }
    }

    [DataContract]
    public class CryptoHashSettings
    {
        [DataMember(Name = "cryptoHashKey")]
        public string CryptoHashKey { get; set; }
    }

    [DataContract]
    public class RedactSettings
    {
        [DataMember(Name = "enablePartialAgesForRedact")]
        public bool EnablePartialAgesForRedact { get; set; }

        [DataMember(Name = "enablePartialDatesForRedact")]
        public bool EnablePartialDatesForRedact { get; set; }
    }

    [DataContract]
    public class EncryptSettings
    {
        [DataMember(Name = "encryptKey")]
        public string EncryptKey { get; set; }
    }

    [DataContract]
    public class SubstituteSettings
    {
        [DataMember(Name = "replaceWith")]
        public string ReplaceWith { get; set; }
    }
}