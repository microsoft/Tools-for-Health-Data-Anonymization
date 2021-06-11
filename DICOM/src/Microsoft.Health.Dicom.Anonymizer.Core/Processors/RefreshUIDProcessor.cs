// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// Replaces the content of a UID with a random one.
    /// The processor makes sure the same original UID will be replaced with the same new UID.
    /// </summary>
    public class RefreshUIDProcessor : IAnonymizerProcessor
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<RefreshUIDProcessor>();

        public static ConcurrentDictionary<string, string> ReplacedUIDs { get; } = new ConcurrentDictionary<string, string>();

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            DicomUID newUID;
            var oldUID = ((DicomElement)item).Get<string>();

            if (ReplacedUIDs.ContainsKey(oldUID))
            {
                newUID = new DicomUID(ReplacedUIDs[oldUID], "Anonymized UID", DicomUidType.Unknown);
            }
            else
            {
                newUID = DicomUIDGenerator.GenerateDerivedFromUUID();
                ReplacedUIDs[oldUID] = newUID.UID;
            }

            var newItem = new DicomUniqueIdentifier(item.Tag, newUID);
            dicomDataset.AddOrUpdate(newItem);

            _logger.LogDebug($"The UID value of DICOM item '{item}' is refreshed.");
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
