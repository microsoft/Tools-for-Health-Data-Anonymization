// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    internal static class Constants
    {
        // InstanceType constants
        internal const string DateTypeName = "date";
        internal const string DateTimeTypeName = "dateTime";
        internal const string DecimalTypeName = "decimal";
        internal const string InstantTypeName = "instant";
        internal const string AgeTypeName = "Age";
        internal const string BundleTypeName = "Bundle";
        internal const string ReferenceTypeName = "Reference";

        // NodeName constants
        internal const string PostalCodeNodeName = "postalCode";
        internal const string ReferenceStringNodeName = "reference";
        internal const string ContainedNodeName = "contained";
        internal const string EntryNodeName = "entry";
        internal const string EntryResourceNodeName = "resource";

        internal const string TagKey = "tag";
        internal const string VRKey = "VR";
        internal const string MethodKey = "method";
        internal const string Parameters = "params";
        internal const string RuleSetting = "setting";

        internal const int DefaultPartitionedExecutionCount = 4;
        internal const int DefaultPartitionedExecutionBatchSize = 1000;

        internal const string GeneralResourceType = "Resource";
        internal const string GeneralDomainResourceType = "DomainResource";

        internal const string RedactDefaultSetting = "redactDefaultSetting";
        internal const string PerturbDefaultSetting = "perturbDefaultSetting";
        internal const string EncryptDefaultSetting = "encryptDefaultSetting";
        internal const string CryptoHashDefaultSetting = "cryptoHashDefaultSetting";
        internal const string DateShiftDefaultSetting = "dateShiftDefaultSetting";
        internal const string SubstituteDefaultSetting = "substituteDefaultSetting";
    }
}
