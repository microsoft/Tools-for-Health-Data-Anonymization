// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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

            // Use runtime key if available, otherwise use configuration key
            var cryptoHashFunction = GetCryptoHashFunction(context);

            if (item is DicomStringElement)
            {
                var hashedValues = ((DicomStringElement)item).Get<string[]>().Select(value => GetCryptoHashString(value, cryptoHashFunction));
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

        private string GetCryptoHashString(string input, CryptoHashFunction cryptoHashFunction)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return cryptoHashFunction.Hash(input);
        }
    }
}
