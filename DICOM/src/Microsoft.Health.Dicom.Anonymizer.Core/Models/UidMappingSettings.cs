// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Models
{
    /// <summary>
    /// Immutable, execution-scoped settings for deterministic UID replacement.
    /// Key and scope are deliberately not exposed in properties or diagnostic text.
    /// </summary>
    public sealed class UidMappingSettings
    {
        public const string HmacSha256128V1 = "hmac-sha256-128-v1";

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly byte[] Domain = Encoding.ASCII.GetBytes("Microsoft.Health.Dicom.Anonymizer/RefreshUID/hmac-sha256-128-v1\0");
        private static readonly Regex UidPattern = new Regex(@"\A(?:0|[1-9][0-9]*)(?:\.(?:0|[1-9][0-9]*))+\z", RegexOptions.CultureInvariant);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte[] _key;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte[] _scope;

        public UidMappingSettings(string mode, string base64Key, string scope)
        {
            if (!string.Equals(mode, HmacSha256128V1, StringComparison.Ordinal))
            {
                throw new ArgumentException("Unsupported UID mapping mode.", nameof(mode));
            }

            if (string.IsNullOrWhiteSpace(scope) || scope.Length > 256)
            {
                throw new ArgumentException("UID mapping scope must be nonblank and at most 256 UTF-8 bytes.", nameof(scope));
            }

            try
            {
                _scope = StrictUtf8.GetBytes(scope);
            }
            catch (EncoderFallbackException)
            {
                throw new ArgumentException("UID mapping scope must be valid UTF-8.", nameof(scope));
            }

            if (_scope.Length > 256)
            {
                throw new ArgumentException("UID mapping scope exceeds 256 UTF-8 bytes.", nameof(scope));
            }

            var key = new byte[32];
            if (base64Key == null || !Convert.TryFromBase64String(base64Key, key, out int written) || written != key.Length)
            {
                CryptographicOperations.ZeroMemory(key);
                throw new ArgumentException("UID mapping key must be Base64 encoding of exactly 32 random bytes.", nameof(base64Key));
            }

            _key = key;
        }

        internal static string[] GetValidatedValues(DicomElement element)
        {
            // Inspect the encoded bytes: string accessors can hide invalid whitespace or padding.
            byte[] bytes = element.Buffer.Data;
            int length = bytes.Length;
            if (length == 0)
            {
                return Array.Empty<string>();
            }

            if (bytes[length - 1] == 0 && length % 2 == 0)
            {
                length--;
            }

            for (int index = 0; index < length; index++)
            {
                byte value = bytes[index];
                if ((value < '0' || value > '9') && value != '.' && value != '\\')
                {
                    throw InvalidUid();
                }
            }

            string[] values = Encoding.ASCII.GetString(bytes, 0, length).Split('\\');
            foreach (string value in values)
            {
                if (value.Length > 64 || (value.Length != 0 && !UidPattern.IsMatch(value)))
                {
                    throw InvalidUid();
                }
            }

            return values;
        }

        internal string Map(string canonicalUid)
        {
            if (canonicalUid.Length == 0)
            {
                return string.Empty;
            }

            byte[] uid = Encoding.ASCII.GetBytes(canonicalUid);
            byte[] message = new byte[Domain.Length + 4 + _scope.Length + 4 + uid.Length];
            Domain.CopyTo(message, 0);
            int index = Domain.Length;
            BinaryPrimitives.WriteUInt32BigEndian(message.AsSpan(index, 4), (uint)_scope.Length);
            index += 4;
            _scope.CopyTo(message, index);
            index += _scope.Length;
            BinaryPrimitives.WriteUInt32BigEndian(message.AsSpan(index, 4), (uint)uid.Length);
            uid.CopyTo(message, index + 4);
            byte[] digest = HMACSHA256.HashData(_key, message);
            return "2.25." + new BigInteger(digest.AsSpan(0, 16), isUnsigned: true, isBigEndian: true).ToString(CultureInfo.InvariantCulture);
        }

        private static AnonymizerOperationException InvalidUid() =>
            new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, "Deterministic UID mapping requires a valid UI value.");
    }
}
