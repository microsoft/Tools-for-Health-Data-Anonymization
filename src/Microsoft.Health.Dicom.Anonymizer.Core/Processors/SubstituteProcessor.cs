// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class SubstituteProcessor : IAnonymizerProcessor
    {
        private DicomSubstituteSetting _defaultSubstituteSetting;

        public SubstituteProcessor(DicomSubstituteSetting defaultSubstituteSettings)
        {
            _defaultSubstituteSetting = defaultSubstituteSettings;
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, DicomBasicInformation basicInfo = null, IDicomAnonymizationSetting settings = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            var substituteSetting = (DicomSubstituteSetting)(settings ?? _defaultSubstituteSetting);

            if (item is DicomOtherByte || item is DicomSequence || item is DicomFragmentSequence)
            {
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"Invalid perturb operation for item {item}");
            }

            try
            {
                if (item is DicomOtherWord)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, ushort.Parse(substituteSetting.ReplaceWith));
                }
                else if (item is DicomOtherLong)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, uint.Parse(substituteSetting.ReplaceWith));
                }
                else if (item is DicomOtherDouble)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, double.Parse(substituteSetting.ReplaceWith));
                }
                else if (item is DicomOtherFloat)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, float.Parse(substituteSetting.ReplaceWith));
                }
                else
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, substituteSetting.ReplaceWith);
                }
            }
            catch (Exception ex)
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Invalid replace value", ex);
            }
        }
    }
}
