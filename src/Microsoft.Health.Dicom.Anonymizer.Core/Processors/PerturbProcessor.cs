// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class PerturbProcessor : IAnonymizerProcessor
    {
        private DicomPerturbSetting _defaultSetting;

        public PerturbProcessor(DicomPerturbSetting defaultSetting)
        {
            EnsureArg.IsNotNull(defaultSetting, nameof(defaultSetting));

            _defaultSetting = defaultSetting;
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, DicomBasicInformation basicInfo = null, IDicomAnonymizationSetting settings = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            if (!IsValidItemForPerturb(item))
            {
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"Perturb is not supported for {item.ValueRepresentation}");
            }

            var perturbSetting = (DicomPerturbSetting)(settings ?? _defaultSetting);

            if (item.ValueRepresentation == DicomVR.AS)
            {
                var values = ((DicomAgeString)item).Get<string[]>().Select(Utility.ParseAge).Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Select(Utility.AgeToString).ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.DS)
            {
                var values = ((DicomDecimalString)item).Get<decimal[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.FL)
            {
                var values = ((DicomFloatingPointSingle)item).Get<float[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.OF)
            {
                var values = ((DicomOtherFloat)item).Get<float[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.FD)
            {
                var values = ((DicomFloatingPointDouble)item).Get<double[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.OD)
            {
                var values = ((DicomOtherDouble)item).Get<double[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.IS)
            {
                var values = ((DicomIntegerString)item).Get<int[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.SL)
            {
                var values = ((DicomSignedLong)item).Get<int[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.SS)
            {
                var values = ((DicomSignedShort)item).Get<short[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.US)
            {
                var values = ((DicomUnsignedShort)item).Get<ushort[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.OW)
            {
                var values = ((DicomOtherWord)item).Get<ushort[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.UL)
            {
                var values = ((DicomUnsignedLong)item).Get<uint[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.OL)
            {
                var values = ((DicomOtherLong)item).Get<uint[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.UV)
            {
                var values = ((DicomUnsignedVeryLong)item).Get<ulong[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.OV)
            {
                var values = ((DicomOtherVeryLong)item).Get<ulong[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.SV)
            {
                var values = ((DicomSignedVeryLong)item).Get<long[]>().Select(x => PerturbFunction.Perturb(x, perturbSetting));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
            }
        }

        public bool IsValidItemForPerturb(DicomItem item)
        {
            var supportedVR = Enum.GetNames(typeof(PerturbSupportedVR)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            return supportedVR.Contains(item.ValueRepresentation.Code) && !(item is DicomFragmentSequence);
        }
    }
}
