// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class MaskProcessor : IAnonymizerProcessor
    {
        private int _maskedLength;

        public MaskProcessor(JObject setting)
        {
            _maskedLength = int.Parse(setting.GetValue("maskedLength", StringComparison.OrdinalIgnoreCase).ToString());
        }

        public bool IsSupported(DicomItem item)
        {
            if (item is DicomStringElement)
            {
                return true;
            }

            return false;
        }

        public static MaskProcessor Create(JObject setting)
        {
            return new MaskProcessor(setting);
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context)
        {
            var mask = new string('*', this._maskedLength);
            if (item is DicomStringElement)
            {
                var values = ((DicomStringElement)item).Get<string[]>().Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Length > _maskedLength ? mask + x[this._maskedLength..] : mask);
                if (values.Count() != 0)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.ToArray());
                }
            }
        }
    }
}
