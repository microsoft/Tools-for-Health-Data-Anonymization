// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Model;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class RefreshUIDProcessor : IAnonymizerProcessor
    {
        public Dictionary<string, string> ReplacedUIDs { get; } = new Dictionary<string, string>();

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            if (!IsValidItemForRefreshUID(item))
            {
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"Invalid refresh UID operation for item {item}");
            }

            string replacedUID;
            DicomUID uid;
            var old = ((DicomElement)item).Get<string>();

            if (ReplacedUIDs.ContainsKey(old))
            {
                replacedUID = ReplacedUIDs[old];
                uid = new DicomUID(replacedUID, "Anonymized UID", DicomUidType.Unknown);
            }
            else
            {
                uid = DicomUIDGenerator.GenerateDerivedFromUUID();
                replacedUID = uid.UID;
                ReplacedUIDs[old] = replacedUID;
            }

            var newItem = new DicomUniqueIdentifier(item.Tag, uid);
            dicomDataset.AddOrUpdate(newItem);
        }

        public bool IsValidItemForRefreshUID(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            var supportedVR = Enum.GetNames(typeof(RefreshUIDSupportedVR)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            return supportedVR.Contains(item.ValueRepresentation.Code);
        }
    }
}
