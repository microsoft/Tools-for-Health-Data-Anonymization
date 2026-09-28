// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Numerics;
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
    /// By default, the length of output string depends on the hash function (e.g. sha256 will output 64 bytes length), you should pay attention to the length limitation of output DICOM file.
    /// Use matchInputStringLength parameter to match the length of the output string to the input for strings
    /// In cryptoHash setting, you can set cryptoHash key and cryptoHash function (only support sha256 for now) for cryptoHash.
    /// </summary>
    public class CryptoHashProcessor : IAnonymizerProcessor
    {
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

            if (!IsSupported(item))
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"CryptoHash is not supported for VR {item.ValueRepresentation}.");
            }

            // Use runtime key if available, otherwise use configuration key
            var cryptoHashFunction = GetCryptoHashFunction(context);

            if (item is DicomStringElement)
            {
                var hashedValues = ((DicomStringElement)item).Get<string[]>().Select(value => GetCryptoHashString(value, item.ValueRepresentation, cryptoHashFunction));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, hashedValues.ToArray());
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

            _logger.LogDebug("CryptoHash completed for tag {Tag} with VR {VR}.", item.Tag, item.ValueRepresentation);
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

        private static string GetCryptoHashString(string input, DicomVR vr, CryptoHashFunction cryptoHashFunction)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            if (input.Length == 0)
            {
                return string.Empty;
            }

            if (vr == DicomVR.UI)
            {
                var hash = cryptoHashFunction.Hash(Encoding.UTF8.GetBytes(input));
                var uidNumber = new BigInteger(hash.Take(16).Concat(new byte[] { 0 }).ToArray());
                return $"2.25.{uidNumber}";
            }

            var maximumLength = vr == DicomVR.AE || vr == DicomVR.CS || vr == DicomVR.SH ? 16 :
                vr == DicomVR.IS ? 9 :
                vr == DicomVR.DS ? 16 :
                vr == DicomVR.LO ? 64 :
                64;
            var outputLength = cryptoHashFunction.MatchInputStringLength ? Math.Min(input.Length, maximumLength) : maximumLength;
            var alphabet = vr == DicomVR.CS ? "0123456789ABCDEF" :
                vr == DicomVR.IS || vr == DicomVR.DS ? "0123456789" :
                "0123456789abcdef";

            if (!cryptoHashFunction.MatchInputStringLength && alphabet == "0123456789abcdef")
            {
                return cryptoHashFunction.Hash(input).Substring(0, outputLength);
            }

            return cryptoHashFunction.HashToAlphabet(input, alphabet, outputLength);
        }
    }
}
