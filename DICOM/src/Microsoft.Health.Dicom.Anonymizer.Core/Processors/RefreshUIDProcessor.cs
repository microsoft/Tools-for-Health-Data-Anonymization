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
    /// <summary>
    /// Replaces the content of a UID with a random one.
    /// The processor makes sure the same original UID will be replaced with the same new UID.
    /// </summary>
    public class RefreshUIDProcessor : IAnonymizerProcessor
    {
        public static Dictionary<string, string> ReplacedUIDs { get; } = new Dictionary<string, string>();

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            DicomUID uid;
            var old = ((DicomElement)item).Get<string>();

            if (ReplacedUIDs.ContainsKey(old))
            {
                uid = new DicomUID(ReplacedUIDs[old], "Anonymized UID", DicomUidType.Unknown);
            }
            else
            {
                uid = DicomUIDGenerator.GenerateDerivedFromUUID();
                ReplacedUIDs[old] = uid.UID;
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
