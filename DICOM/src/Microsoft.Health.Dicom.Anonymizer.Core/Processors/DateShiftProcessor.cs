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
using Microsoft.Health.Dicom.DeID.SharedLib.Settings;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// With this method, the input date/dateTime/ value will be shifted within a specific range.
    /// Dateshift function can only be used for date (DA) and datetime (DT) types.
    /// In rule setting, customers can define dateShiftRange, DateShiftKey and dateShiftScope.
    /// </summary>
    public class DateShiftProcessor : IAnonymizerProcessor
    {
        private readonly DateShiftFunction _dateShiftFunction;
        private readonly DateShiftScope _dateShiftScope = DateShiftScope.SopInstance;
        private static readonly HashSet<string> _supportedVR = Enum.GetNames(typeof(DateShiftSupportedVR)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<DateShiftProcessor>();

        public DateShiftProcessor(JObject settingObject)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            var settingFactory = new AnonymizerSettingsFactory();
            var dateShiftSetting = settingFactory.CreateAnonymizerSetting<DateShiftSetting>(settingObject);
            _dateShiftFunction = new DateShiftFunction(dateShiftSetting);
            if (settingObject.TryGetValue("DateShiftScope", StringComparison.OrdinalIgnoreCase, out JToken scope))
            {
                _dateShiftScope = (DateShiftScope)Enum.Parse(typeof(DateShiftScope), scope.ToString());
            }
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));
            EnsureArg.IsNotNull(context, nameof(context));

            _dateShiftFunction.DateShiftKeyPrefix = _dateShiftScope switch
            {
                DateShiftScope.StudyInstance => context.StudyInstanceUID ?? string.Empty,
                DateShiftScope.SeriesInstance => context.SeriesInstanceUID ?? string.Empty,
                DateShiftScope.SopInstance => context.SopInstanceUID ?? string.Empty,
                _ => string.Empty,
            };
            if (item.ValueRepresentation == DicomVR.DA)
            {
                var values = Utility.ParseDicomDate((DicomDate)item)
                    .Where(x => !DateTimeUtility.IndicateAgeOverThreshold(x)) // Age over 89 will be redacted.
                    .Select(_dateShiftFunction.ShiftDateTime);

                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Select(Utility.GenerateDicomDateString).ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.DT)
            {
                var values = Utility.ParseDicomDateTime((DicomDateTime)item);
                var results = new List<string>();
                foreach (var dateObject in values)
                {
                    dateObject.DateValue = _dateShiftFunction.ShiftDateTime(dateObject.DateValue);
                    if (!DateTimeUtility.IndicateAgeOverThreshold(dateObject.DateValue))
                    {
                        results.Add(Utility.GenerateDicomDateTimeString(dateObject));
                    }
                }

                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, results.ToArray());
            }
            else
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"DateShift is not supported for {item.ValueRepresentation}.");
            }

            _logger.LogDebug($"The value of DICOM item '{item}' is shifted.");
        }

        public bool IsSupportedVR(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return _supportedVR.Contains(item.ValueRepresentation.Code);
        }
    }
}
