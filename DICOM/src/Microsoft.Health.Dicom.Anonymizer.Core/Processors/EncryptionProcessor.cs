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
using Microsoft.Health.DeID.SharedLib.Settings;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class EncryptionProcessor : IAnonymizerProcessor
    {
        private readonly EncryptFunction _encryptFunction;

        public EncryptionProcessor(IDicomAnonymizationSetting ruleSetting = null)
        {
            var setting = (DicomEncryptionSetting)(ruleSetting ?? new DicomEncryptionSetting());
            var key = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(setting.EncryptKey) ? Guid.NewGuid().ToString("N") : setting.EncryptKey);
            _encryptFunction = new EncryptFunction(new EncryptionSetting() { AesKey = key });
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            if (dicomDataset.AutoValidate && !IsValidItemForEncrypt(item))
            {
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"Encrypt is not supported for {item.ValueRepresentation}");
            }

            var encoding = DicomEncoding.Default;
            try
            {
                if (item is DicomStringElement)
                {
                    var encryptedValues = ((DicomStringElement)item).Get<string[]>().Where(x => !string.IsNullOrEmpty(x)).Select(x => Convert.ToBase64String(_encryptFunction.EncryptContentWithAES(encoding.GetBytes(x))));
                    if (encryptedValues.Count() != 0)
                    {
                        dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, encryptedValues.ToArray());
                    }
                }
                else if (item is DicomOtherByte)
                {
                    var valueBytes = ((DicomOtherByte)item).Get<byte[]>();
                    var encryptesBytes = _encryptFunction.EncryptContentWithAES(valueBytes);
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, encryptesBytes);
                }
                else if (item is DicomFragmentSequence)
                {
                    List<byte[]> results = new List<byte[]>();
                    var enumerator = ((DicomFragmentSequence)item).GetEnumerator();

                    var element = item.ValueRepresentation == DicomVR.OW
                        ? (DicomFragmentSequence)new DicomOtherWordFragment(item.Tag)
                        : new DicomOtherByteFragment(item.Tag);

                    while (enumerator.MoveNext())
                    {
                        element.Fragments.Add(new MemoryByteBuffer(_encryptFunction.EncryptContentWithAES(enumerator.Current.Data)));
                    }

                    dicomDataset.AddOrUpdate(element);
                }
                else
                {
                    throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"Encrypt is not supported for {item.ValueRepresentation}");
                }
            }
            catch (Exception ex)
            {
                if (ex is DicomValidationException)
                {
                    throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"Encrypt is not supported for {item.ValueRepresentation}", ex);
                }

                throw;
            }
        }

        public bool IsValidItemForEncrypt(DicomItem item)
        {
            var supportedVR = Enum.GetNames(typeof(EncryptSupportedVR)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            return supportedVR.Contains(item.ValueRepresentation.Code) || item is DicomFragmentSequence;
        }
    }
}
