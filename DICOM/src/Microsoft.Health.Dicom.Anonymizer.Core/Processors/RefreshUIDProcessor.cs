// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class RefreshUIDProcessor : IAnonymizerProcessor
    {
        public Dictionary<string, string> ReplacedUIDs { get; } = new Dictionary<string, string>();

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

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

        public bool IsSupportedVR(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            if (item.ValueRepresentation != DicomVR.UI)
            {
                return false;
            }

            return true;
        }
    }
}
