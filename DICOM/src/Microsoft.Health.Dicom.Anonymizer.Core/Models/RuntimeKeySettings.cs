// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Anonymizer.Core.Models
{
    /// <summary>
    /// Provides optional runtime key values that can override configuration-based keys
    /// when de-identifying a specific dataset.
    /// </summary>
    public class RuntimeKeySettings
    {
        /// <summary>
        /// Gets or sets the runtime key for cryptographic hashing operations.
        /// If null, the processor will use the key from configuration.
        /// </summary>
        public string CryptoHashKey { get; set; }

        /// <summary>
        /// Gets or sets the runtime key for date shifting operations.
        /// If null, the processor will use the key from configuration.
        /// </summary>
        public string DateShiftKey { get; set; }

        /// <summary>
        /// Gets or sets the runtime key for encryption operations.
        /// If null, the processor will use the key from configuration.
        /// </summary>
        public string EncryptKey { get; set; }
    }
}