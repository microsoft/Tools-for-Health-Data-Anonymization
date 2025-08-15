// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Anonymizer.Core.Models
{
    /// <summary>
    /// Provides optional runtime seed values that can override configuration-based seeds
    /// when de-identifying a specific dataset.
    /// </summary>
    public class RuntimeSeedSettings
    {
        /// <summary>
        /// Gets or sets the runtime seed key for cryptographic hashing operations.
        /// If null, the processor will use the seed from configuration.
        /// </summary>
        public string CryptoHashKey { get; set; }

        /// <summary>
        /// Gets or sets the runtime seed key for date shifting operations.
        /// If null, the processor will use the seed from configuration.
        /// </summary>
        public string DateShiftKey { get; set; }

        /// <summary>
        /// Gets or sets the runtime seed key for encryption operations.
        /// If null, the processor will use the seed from configuration.
        /// </summary>
        public string EncryptKey { get; set; }
    }
}