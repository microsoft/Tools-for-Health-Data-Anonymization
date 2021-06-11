// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// With perturb rule, you can replace specific values by adding noise.
    /// Perturb function can be used for numeric values (ushort, short, uint, int, ulong, long, decimal, double, float).
    /// </summary>
    public class PerturbProcessor : IAnonymizerProcessor
    {
        private PerturbFunction _perturbFunction;
        private static readonly HashSet<string> _supportedVR = Enum.GetNames(typeof(PerturbSupportedVR)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<PerturbProcessor>();

        private readonly Dictionary<DicomVR, VRTypes> _numericValueTypeMapping = new Dictionary<DicomVR, VRTypes>()
        {
            { DicomVR.DS, new VRTypes() { ElementType = typeof(DicomDecimalString), ValueType = typeof(decimal[]) } },
            { DicomVR.FL, new VRTypes() { ElementType = typeof(DicomFloatingPointSingle), ValueType = typeof(float[]) } },
            { DicomVR.OF, new VRTypes() { ElementType = typeof(DicomOtherFloat), ValueType = typeof(float[]) } },
            { DicomVR.FD, new VRTypes() { ElementType = typeof(DicomFloatingPointDouble), ValueType = typeof(double[]) } },
            { DicomVR.OD, new VRTypes() { ElementType = typeof(DicomOtherDouble), ValueType = typeof(double[]) } },
            { DicomVR.IS, new VRTypes() { ElementType = typeof(DicomIntegerString), ValueType = typeof(int[]) } },
            { DicomVR.SL, new VRTypes() { ElementType = typeof(DicomSignedLong), ValueType = typeof(int[]) } },
            { DicomVR.SS, new VRTypes() { ElementType = typeof(DicomSignedShort), ValueType = typeof(short[]) } },
            { DicomVR.US, new VRTypes() { ElementType = typeof(DicomUnsignedShort), ValueType = typeof(ushort[]) } },
            { DicomVR.OW, new VRTypes() { ElementType = typeof(DicomOtherWord), ValueType = typeof(ushort[]) } },
            { DicomVR.UL, new VRTypes() { ElementType = typeof(DicomUnsignedLong), ValueType = typeof(uint[]) } },
            { DicomVR.OL, new VRTypes() { ElementType = typeof(DicomOtherLong), ValueType = typeof(uint[]) } },
            { DicomVR.UV, new VRTypes() { ElementType = typeof(DicomUnsignedVeryLong), ValueType = typeof(ulong[]) } },
            { DicomVR.OV, new VRTypes() { ElementType = typeof(DicomOtherVeryLong), ValueType = typeof(ulong[]) } },
            { DicomVR.SV, new VRTypes() { ElementType = typeof(DicomSignedVeryLong), ValueType = typeof(long[]) } },
        };

        public PerturbProcessor(JObject settingObject)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            var settingFactory = new AnonymizerSettingsFactory();
            var perturbSetting = settingFactory.CreateAnonymizerSetting<PerturbSetting>(settingObject);
            _perturbFunction = new PerturbFunction(perturbSetting);
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            if (item.ValueRepresentation == DicomVR.AS)
            {
                var values = ((DicomAgeString)item).Get<string[]>().Select(Utility.ParseAge).Select(x => _perturbFunction.Perturb(x));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Select(Utility.AgeToString).Where(x => x != null).ToArray());
            }
            else
            {
                if (!_numericValueTypeMapping.ContainsKey(item.ValueRepresentation))
                {
                    throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"Perturb is not supported for {item.ValueRepresentation}.");
                }

                var elementType = _numericValueTypeMapping[item.ValueRepresentation].ElementType;
                var valueType = _numericValueTypeMapping[item.ValueRepresentation].ValueType;

                // Get numeric value using reflection.
                var valueObj = elementType.GetMethod("Get").MakeGenericMethod(valueType).Invoke(item, new object[] { -1 });
                PerturbNumericValue(dicomDataset, item, valueObj as Array);
            }

            _logger.LogDebug($"The value of DICOM item '{item}' is perturbed.");
        }

        private void PerturbNumericValue(DicomDataset dicomDataset, DicomItem item, Array values)
        {
            if (values == null || values.Length == 0)
            {
                return;
            }

            var valueType = values.GetValue(0).GetType();
            if (valueType == typeof(decimal))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<decimal>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(double))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<double>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(float))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<float>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(int))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<int>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(uint))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<uint>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(short))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<short>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(ushort))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<ushort>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(long))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<long>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(ulong))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<ulong>().Select(_perturbFunction.Perturb).ToArray());
            }
            else
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<string>().Select(_perturbFunction.Perturb).ToArray());
            }
        }

        public bool IsSupportedVR(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return _supportedVR.Contains(item.ValueRepresentation.Code) && !(item is DicomFragmentSequence);
        }
    }
}
