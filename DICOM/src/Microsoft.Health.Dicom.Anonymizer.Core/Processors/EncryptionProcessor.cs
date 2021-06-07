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
using Microsoft.Health.DeID.SharedLib.Settings;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class EncryptionProcessor : IAnonymizerProcessor
    {
        private readonly EncryptFunction _encryptFunction;

        public EncryptionProcessor(JObject settingObject, IAnonymizerSettingsFactory settingFactory = null)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            settingFactory ??= new AnonymizerSettingsFactory();
            var encryptionSetting = settingFactory.CreateAnonymizerSetting<EncryptionSetting>(settingObject);
            _encryptFunction = new EncryptFunction(encryptionSetting);
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            if (dicomDataset.AutoValidate && !IsValidItemForEncrypt(item))
            {
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"Encrypt is not supported for {item.ValueRepresentation}");
            }

            try
            {
                if (item is DicomStringElement)
                {
                    var encryptedValues = ((DicomStringElement)item).Get<string[]>().Where(x => !string.IsNullOrEmpty(x)).Select(x => EncryptToBase64String(x));
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
            catch (DicomValidationException ex)
            {
                // The length for encrypted output will varies, which may invalid even we check VR in advance.
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"Encrypt is not supported for {item.ValueRepresentation}", ex);
            }
        }

        private string EncryptToBase64String(string plainString)
        {
            return Convert.ToBase64String(_encryptFunction.EncryptContentWithAES(DicomEncoding.Default.GetBytes(plainString)));
        }

        public bool IsValidItemForEncrypt(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            var supportedVR = Enum.GetNames(typeof(EncryptSupportedVR)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            return supportedVR.Contains(item.ValueRepresentation.Code) || item is DicomFragmentSequence;
        }
    }
}
