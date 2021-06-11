// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dicom;
using Dicom.IO.Buffer;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DeID.SharedLib.Settings;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// This function hash the value and outputs a Hex encoded representation.
    /// The length of output string depends on the hash function (e.g. sha256 will output 64 bytes length), you should pay attention to the length limitation of output DICOM file.
    /// In cryptoHash setting, you can set cryptoHash key and cryptoHash function (only support sha256 for now) for cryptoHash.
    /// </summary>
    public class CryptoHashProcessor : IAnonymizerProcessor
    {
        private readonly CryptoHashFunction _cryptoHashFunction;
        private static readonly HashSet<string> _supportedVR = Enum.GetNames(typeof(CryptoHashSupportedVR)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<CryptoHashProcessor>();

        public CryptoHashProcessor(JObject settingObject)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            var settingFactory = new AnonymizerSettingsFactory();
            var cryptoHashSetting = settingFactory.CreateAnonymizerSetting<CryptoHashSetting>(settingObject);
            _cryptoHashFunction = new CryptoHashFunction(cryptoHashSetting);
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            if (item is DicomStringElement)
            {
                var encryptedValues = ((DicomStringElement)item).Get<string[]>().Select(GetCryptoHashString);
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, encryptedValues.ToArray());
            }
            else if (item is DicomOtherByte)
            {
                var valueBytes = ((DicomOtherByte)item).Get<byte[]>();
                var encryptesBytes = _cryptoHashFunction.ComputeHmacSHA256Hash(valueBytes);
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, encryptesBytes);
            }
            else if (item is DicomFragmentSequence)
            {
                var enumerator = ((DicomFragmentSequence)item).GetEnumerator();

                var element = item.ValueRepresentation == DicomVR.OW
                    ? (DicomFragmentSequence)new DicomOtherWordFragment(item.Tag)
                    : new DicomOtherByteFragment(item.Tag);

                while (enumerator.MoveNext())
                {
                    element.Fragments.Add(new MemoryByteBuffer(_cryptoHashFunction.ComputeHmacSHA256Hash(enumerator.Current.Data)));
                }

                dicomDataset.AddOrUpdate(element);
            }
            else
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"CryptoHash is not supported for {item.ValueRepresentation}.");
            }

            _logger.LogDebug($"The value of DICOM item '{item}' is cryptoHashed.");
        }

        public bool IsSupportedVR(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return _supportedVR.Contains(item.ValueRepresentation.Code) || item is DicomFragmentSequence;
        }

        public string GetCryptoHashString(string input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            var resultBytes = _cryptoHashFunction.ComputeHmacSHA256Hash(Encoding.UTF8.GetBytes(input));
            return string.Concat(resultBytes.Select(b => b.ToString("x2")));
        }
    }
}
