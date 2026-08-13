// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Anonymizer.Common;
using Microsoft.Health.Anonymizer.Common.Settings;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// This function hash the value and outputs a Hex encoded representation.
    /// The generated value is constrained to the value representation (VR) of the target DICOM item, which means that it is
    /// truncated to the maximum length of the VR (e.g. 16 characters for SH) and converted to the character repertoire of the VR
    /// (e.g. digits only for DS, IS and UI). Truncation reduces the collision resistance of the hash to the retained characters
    /// (e.g. 16 hex characters retain 64 bits), which is a deliberate tradeoff to produce DICOM conformant output.
    /// Hashing stays deterministic: the same input, key and target VR always produce the same output.
    /// Use matchInputStringLength parameter to match the length of the output string to the input for strings
    /// In cryptoHash setting, you can set cryptoHash key and cryptoHash function (only support sha256 for now) for cryptoHash.
    /// </summary>
    public class CryptoHashProcessor : IAnonymizerProcessor
    {
        // Value representations whose values are restricted to digits (and periods for UI).
        private static readonly HashSet<DicomVR> NumericStringVR = new HashSet<DicomVR>
        {
            DicomVR.DS,
            DicomVR.IS,
            DicomVR.UI,
        };

        // Integer strings are limited to values that fit in a 32-bit signed integer.
        private const int MaxIntegerStringLength = 9;

        private readonly CryptoHashFunction _cryptoHashFunction;
        private readonly CryptoHashSetting _cryptoHashSetting;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<CryptoHashProcessor>();

        public CryptoHashProcessor(JObject settingObject)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            var settingFactory = new AnonymizerSettingsFactory();
            _cryptoHashSetting = settingFactory.CreateAnonymizerSetting<CryptoHashSetting>(settingObject);
            _cryptoHashFunction = new CryptoHashFunction(_cryptoHashSetting);
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            // Use runtime key if available, otherwise use configuration key
            var cryptoHashFunction = GetCryptoHashFunction(context);

            if (item is DicomStringElement)
            {
                var vr = item.ValueRepresentation;
                var hashedValues = ((DicomStringElement)item).Get<string[]>().Select(value => GetCryptoHashString(value, cryptoHashFunction, vr));

                try
                {
                    dicomDataset.AddOrUpdate(vr, item.Tag, hashedValues.ToArray());
                }
                catch (DicomValidationException)
                {
                    // The exception message intentionally excludes both the source value and the generated value.
                    throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"CryptoHash cannot generate a value conformant with value representation {vr} for tag {item.Tag}. Use another anonymization method (e.g. dateShift or redact) for this tag.");
                }
            }
            else if (item is DicomOtherByte)
            {
                var valueBytes = ((DicomOtherByte)item).Get<byte[]>();
                var hashedBytes = cryptoHashFunction.Hash(valueBytes);
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, hashedBytes);
            }
            else if (item is DicomFragmentSequence)
            {
                var element = item.ValueRepresentation == DicomVR.OW
                    ? (DicomFragmentSequence)new DicomOtherWordFragment(item.Tag)
                    : new DicomOtherByteFragment(item.Tag);

                foreach (var fragment in (DicomFragmentSequence)item)
                {
                    element.Fragments.Add(new MemoryByteBuffer(cryptoHashFunction.Hash(fragment.Data)));
                }

                dicomDataset.AddOrUpdate(element);
            }
            else
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"CryptoHash is not supported for {item.ValueRepresentation}.");
            }

            _logger.LogDebug($"The value of DICOM item '{item}' is cryptoHashed.");
        }

        public bool IsSupported(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return DicomDataModel.CryptoHashSupportedVR.Contains(item.ValueRepresentation) || item is DicomFragmentSequence;
        }

        public string GetCryptoHashString(string input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return _cryptoHashFunction.Hash(input);
        }

        private CryptoHashFunction GetCryptoHashFunction(ProcessContext context)
        {
            // If runtime keys are provided and contain a crypto hash key, use it
            if (context?.RuntimeKeys?.CryptoHashKey != null)
            {
                var runtimeSetting = new CryptoHashSetting
                {
                    CryptoHashKey = context.RuntimeKeys.CryptoHashKey,
                    CryptoHashType = _cryptoHashSetting.CryptoHashType,
                    MatchInputStringLength = _cryptoHashSetting.MatchInputStringLength,
                };
                return new CryptoHashFunction(runtimeSetting);
            }

            // Fall back to configuration-based function
            return _cryptoHashFunction;
        }

        private string GetCryptoHashString(string input, CryptoHashFunction cryptoHashFunction, DicomVR valueRepresentation)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return ConformToValueRepresentation(cryptoHashFunction.Hash(input), valueRepresentation);
        }

        /// <summary>
        /// Constrains a hashed value to the character repertoire and the maximum length of the given value representation.
        /// </summary>
        private static string ConformToValueRepresentation(string hashedValue, DicomVR valueRepresentation)
        {
            var value = hashedValue;

            if (NumericStringVR.Contains(valueRepresentation))
            {
                value = ConvertToDigits(value);
            }
            else if (valueRepresentation == DicomVR.CS)
            {
                // Code strings only allow uppercase characters, digits, space and underscore.
                value = value.ToUpperInvariant();
            }

            var maximumLength = valueRepresentation.MaximumLength == 0 ? int.MaxValue : (int)valueRepresentation.MaximumLength;
            if (valueRepresentation == DicomVR.IS)
            {
                maximumLength = Math.Min(maximumLength, MaxIntegerStringLength);
            }

            return value.Length > maximumLength ? value.Substring(0, maximumLength) : value;
        }

        /// <summary>
        /// Deterministically maps a hashed value to digits only. Digits are preserved, other characters are mapped to
        /// the decimal digit of their hexadecimal value. The leading digit is never zero, since leading zeros are not
        /// allowed in decimal, integer and unique identifier strings.
        /// </summary>
        private static string ConvertToDigits(string hashedValue)
        {
            var digits = new StringBuilder(hashedValue.Length);
            foreach (var character in hashedValue)
            {
                var hexValue = character >= '0' && character <= '9'
                    ? character - '0'
                    : char.ToLowerInvariant(character) - 'a' + 10;
                digits.Append((char)('0' + (hexValue % 10)));
            }

            if (digits.Length > 0 && digits[0] == '0')
            {
                digits[0] = '1';
            }

            return digits.ToString();
        }
    }
}
