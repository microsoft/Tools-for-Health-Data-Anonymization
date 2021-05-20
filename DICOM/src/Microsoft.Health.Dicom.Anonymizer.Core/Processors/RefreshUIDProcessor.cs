// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class RefreshUIDProcessor : IAnonymizerProcessor
    {
        public Dictionary<string, string> ReplacedUIDs { get; } = new Dictionary<string, string>();

        public void Process(DicomDataset dicomDataset, DicomItem item, DicomBasicInformation basicInfo = null, IDicomAnonymizationSetting settings = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            if (!(item is DicomElement) || item.ValueRepresentation != DicomVR.UI)
            {
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"Invalid refresh UID operation for item {item}");
            }

            string replaced;
            DicomUID uid;
            var old = ((DicomElement)item).Get<string>();

            if (ReplacedUIDs.ContainsKey(old))
            {
                replaced = ReplacedUIDs[old];
                uid = new DicomUID(replaced, "Anonymized UID", DicomUidType.Unknown);
            }
            else
            {
                uid = DicomUIDGenerator.GenerateDerivedFromUUID();
                replaced = uid.UID;
                ReplacedUIDs[old] = replaced;
            }

            var newItem = new DicomUniqueIdentifier(item.Tag, uid);
            dicomDataset.AddOrUpdate(newItem);
        }
    }
}
