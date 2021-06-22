// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// We use AES-CBC algorithm to transform the value with an encryption key, and then replace the original value with a Base64 encoded representation of the encrypted value.
    /// </summary>
    public class EncryptProcessor : IAnonymizerProcessor
    {
        private readonly EncryptFunction _encryptFunction;
        private static readonly HashSet<DicomVR> _supportedVR = DicomDataModel.EncryptSupportedVR;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<EncryptProcessor>();

        public EncryptProcessor(JObject settingObject)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            var settingFactory = new AnonymizerSettingsFactory();
            var encryptionSetting = settingFactory.CreateAnonymizerSetting<EncryptSetting>(settingObject);
            _encryptFunction = new EncryptFunction(encryptionSetting);
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

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
                    var element = item.ValueRepresentation == DicomVR.OW
                        ? (DicomFragmentSequence)new DicomOtherWordFragment(item.Tag)
                        : new DicomOtherByteFragment(item.Tag);

                    foreach (var fragment in (DicomFragmentSequence)item)
                    {
                        element.Fragments.Add(new MemoryByteBuffer(_encryptFunction.EncryptContentWithAES(fragment.Data)));
                    }

                    dicomDataset.AddOrUpdate(element);
                }
                else
                {
                    throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"Encrypt is not supported for {item.ValueRepresentation}.");
                }

                _logger.LogDebug($"The value of DICOM item '{item}' is encrypted.");
            }
            catch (DicomValidationException ex)
            {
                // The length for encrypted output will varies, which may invalid even we check VR in advance.
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"Encrypt is not supported for {item.ValueRepresentation}.", ex);
            }
        }

        private string EncryptToBase64String(string plainString)
        {
            return Convert.ToBase64String(_encryptFunction.EncryptContentWithAES(DicomEncoding.Default.GetBytes(plainString)));
        }

        public bool IsSupported(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return _supportedVR.Contains(item.ValueRepresentation) || item is DicomFragmentSequence;
        }
    }
}
