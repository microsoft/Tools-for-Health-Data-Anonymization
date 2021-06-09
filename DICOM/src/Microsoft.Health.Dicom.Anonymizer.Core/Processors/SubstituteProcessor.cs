// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class SubstituteProcessor : IAnonymizerProcessor
    {
        private readonly string _replaceWith = "Anonymous";

        public SubstituteProcessor(JObject settingObject)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            if (settingObject.TryGetValue("replaceWith", StringComparison.OrdinalIgnoreCase, out JToken replaced))
            {
                _replaceWith = replaced.ToString();
            }
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            try
            {
                if (item.ValueRepresentation == DicomVR.OW && !(item is DicomFragmentSequence))
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, ushort.Parse(_replaceWith));
                }
                else if (item.ValueRepresentation == DicomVR.OL)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, uint.Parse(_replaceWith));
                }
                else if (item.ValueRepresentation == DicomVR.OD)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, double.Parse(_replaceWith));
                }
                else if (item.ValueRepresentation == DicomVR.OF)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, float.Parse(_replaceWith));
                }
                else
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, _replaceWith);
                }
            }
            catch (Exception ex)
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Invalid replace value", ex);
            }
        }

        public bool IsSupportedVR(DicomItem item)
        {
            if (item is DicomOtherByte || item is DicomSequence || item is DicomFragmentSequence)
            {
                return false;
            }

            return true;
        }
    }
}
