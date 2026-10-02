// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Linq;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// Replaces a UID using optional execution-scoped deterministic settings,
    /// or the legacy process-local random mapping when no settings are supplied.
    /// </summary>
    public class RefreshUIDProcessor : IAnonymizerProcessor
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<RefreshUIDProcessor>();

        public static ConcurrentDictionary<string, DicomUID> ReplacedUIDs { get; } = new ConcurrentDictionary<string, DicomUID>();

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            if (item.ValueRepresentation == DicomVR.UI)
            {
                if (DicomUtility.IsInvariantUid(item.Tag))
                {
                    throw new AnonymizerOperationException(
                        DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod,
                        $"RefreshUID is not supported for invariant tag {item.Tag} with VR {item.ValueRepresentation}.");
                }

                var mapping = context?.UidMapping;
                var oldUIDValues = mapping == null
                    ? ((DicomElement)item).Get<string[]>()
                    : UidMappingSettings.GetValidatedValues((DicomElement)item);
                var newUIDValues = oldUIDValues
                    .Select(value => mapping == null
                        ? ReplacedUIDs.GetOrAdd(value, _ => DicomUIDGenerator.GenerateDerivedFromUUID()).UID
                        : mapping.Map(value))
                    .ToArray();
                var newItem = new DicomUniqueIdentifier(item.Tag, newUIDValues);
                dicomDataset.AddOrUpdate(newItem);

                _logger.LogDebug(
                    "The UID value for tag ({Group:X4},{Element:X4}) with VR {VR} is refreshed.",
                    item.Tag.Group,
                    item.Tag.Element,
                    item.ValueRepresentation.Code);
            }
            else
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"RefreshUID is not supported for {item.ValueRepresentation}.");
            }
        }

        public bool IsSupported(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return DicomDataModel.RefreshUIDSupportedVR.Contains(item.ValueRepresentation) && !DicomUtility.IsInvariantUid(item.Tag);
        }
    }
}
