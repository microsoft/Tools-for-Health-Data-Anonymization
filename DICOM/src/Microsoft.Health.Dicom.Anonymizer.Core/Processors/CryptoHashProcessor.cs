// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;
using Dicom;
using Dicom.IO.Buffer;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class CryptoHashProcessor : IAnonymizerProcessor
    {
        private readonly CryptoHashFunction _cryptoHashFunction;

        public CryptoHashProcessor(IDicomAnonymizationSetting ruleSetting = null)
        {
             var setting = (DicomCryptoHashSetting)(ruleSetting ?? new DicomCryptoHashSetting());
             var cryptoHashKey = Encoding.UTF8.GetBytes(setting.CryptoHashKey);
             _cryptoHashFunction = new CryptoHashFunction(cryptoHashKey);
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            var test = item.GetType().GetMethods();
            if (dicomDataset.AutoValidate && !IsValidItemForCryptoHash(item))
            {
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"CryptoHash is not supported for {item.ValueRepresentation}");
            }

            var encoding = Encoding.UTF8;
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
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"CryptoHash is not supported for {item.ValueRepresentation}");
            }
        }

        public bool IsValidItemForCryptoHash(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            var supportedVR = Enum.GetNames(typeof(CryptoHashSupportedVR)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            return supportedVR.Contains(item.ValueRepresentation.Code) || item is DicomFragmentSequence;
        }

        public string GetCryptoHashString(string input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            var resultBytes = _cryptoHashFunction.ComputeHmacSHA256Hash(Encoding.UTF8.GetBytes(input));
            return resultBytes == null ? null : string.Concat(resultBytes.Select(b => b.ToString("x2")));
        }
    }
}
