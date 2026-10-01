// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Rules;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        internal const int MaximumSequenceDepth = 64;

        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly AnonymizerEngineOptions _anonymizerSettings;
        private readonly bool _requireRuntimeKeys;
        private readonly AnonymizerRule[] _rules;
        private readonly bool _usesCryptoHash;
        private readonly bool _usesDateShift;
        private readonly bool _usesEncrypt;

        public AnonymizerEngine(string configFilePath = "configuration.json", AnonymizerEngineOptions anonymizerSettings = null, IAnonymizerRuleFactory ruleFactory = null, IAnonymizerProcessorFactory processorFactory = null)
            : this(configFilePath, anonymizerSettings, ruleFactory, processorFactory, false)
        {
        }

        public AnonymizerEngine(string configFilePath, bool requireRuntimeKeys)
            : this(configFilePath, null, null, null, requireRuntimeKeys)
        {
        }

        public AnonymizerEngine(string configFilePath, AnonymizerEngineOptions anonymizerSettings, IAnonymizerRuleFactory ruleFactory, IAnonymizerProcessorFactory processorFactory, bool requireRuntimeKeys)
            : this(AnonymizerConfigurationManager.CreateFromJsonFile(configFilePath), anonymizerSettings, ruleFactory, processorFactory, requireRuntimeKeys)
        {
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, AnonymizerEngineOptions anonymizerSettings = null, IAnonymizerRuleFactory ruleFactory = null, IAnonymizerProcessorFactory processorFactory = null)
            : this(configurationManager, anonymizerSettings, ruleFactory, processorFactory, false)
        {
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, bool requireRuntimeKeys)
            : this(configurationManager, null, null, null, requireRuntimeKeys)
        {
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, AnonymizerEngineOptions anonymizerSettings, IAnonymizerRuleFactory ruleFactory, IAnonymizerProcessorFactory processorFactory, bool requireRuntimeKeys)
        {
            EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));

            _anonymizerSettings = anonymizerSettings ?? new AnonymizerEngineOptions();
            _requireRuntimeKeys = requireRuntimeKeys;
            (_usesCryptoHash, _usesDateShift, _usesEncrypt) = GetConfiguredKeyedMethods(configurationManager.Configuration.RuleContent);

            ruleFactory ??= new AnonymizerRuleFactory(configurationManager.Configuration, processorFactory ?? new DicomProcessorFactory());
            _rules = ruleFactory.CreateDicomAnonymizationRules(configurationManager.Configuration.RuleContent);
            _logger.LogDebug("Successfully initialized anonymizer engine.");
        }

        public void AnonymizeDataset(DicomDataset dataset)
        {
            AnonymizeDataset(dataset, null);
        }

        public void AnonymizeDataset(DicomDataset dataset, RuntimeKeySettings runtimeKeySettings)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            ValidateRequiredRuntimeKeys(runtimeKeySettings);

            ValidateSequenceDepth(dataset);

            // Validate input dataset.
            if (_anonymizerSettings.ValidateInput)
            {
                dataset.Validate();
            }

            var nestedOperations = ValidateProcessing(dataset, runtimeKeySettings);
            ProcessDataset(dataset, runtimeKeySettings);
            foreach (var (nestedDataset, rule) in nestedOperations)
            {
                DicomUtility.DisableAutoValidation(nestedDataset);
                var context = InitContext(nestedDataset, runtimeKeySettings);
                rule.Handle(nestedDataset, context);
            }

            // Validate output dataset.
            if (_anonymizerSettings.ValidateOutput)
            {
                dataset.Validate();
            }
        }

        public DicomFile AnonymizeFile(DicomFile dicomFile)
        {
            return AnonymizeFile(dicomFile, null);
        }

        public DicomFile AnonymizeFile(DicomFile dicomFile, RuntimeKeySettings runtimeKeySettings)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));

            ValidateSequenceDepth(dicomFile.Dataset);
            ValidateFileMetaIdentity(dicomFile);

            var cloneSource = new DicomFile(dicomFile.Dataset.Clone());
            cloneSource.FileMetaInfo.Clear();
            cloneSource.FileMetaInfo.Add(new DicomFileMetaInformation(dicomFile.FileMetaInfo));
            using var stream = new MemoryStream();
            cloneSource.Save(stream);
            stream.Position = 0;
            var output = DicomFile.Open(stream, FileReadOption.ReadAll);

            AnonymizeFileInPlace(output, runtimeKeySettings);
            return output;
        }

        public void AnonymizeFileInPlace(DicomFile dicomFile)
        {
            AnonymizeFileInPlace(dicomFile, null);
        }

        public void AnonymizeFileInPlace(DicomFile dicomFile, RuntimeKeySettings runtimeKeySettings)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));

            ValidateSequenceDepth(dicomFile.Dataset);
            ValidateFileMetaIdentity(dicomFile);

            var transferSyntax = dicomFile.FileMetaInfo.TransferSyntax;
            AnonymizeDataset(dicomFile.Dataset, runtimeKeySettings);

            var (_, sopInstanceUid) = GetRequiredDatasetIdentity(dicomFile.Dataset);
            dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = sopInstanceUid;
            dicomFile.FileMetaInfo.TransferSyntax = transferSyntax;
            ValidateFileMetaIdentity(dicomFile);
        }

        private ProcessContext InitContext(DicomDataset dataset, RuntimeKeySettings runtimeKeySettings)
        {
            var context = new ProcessContext
            {
                StudyInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                SopInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                SeriesInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                RuntimeKeys = runtimeKeySettings,
            };
            return context;
        }

        private void ProcessDataset(DicomDataset dataset, RuntimeKeySettings runtimeKeySettings)
        {
            DicomUtility.DisableAutoValidation(dataset);

            var context = InitContext(dataset, runtimeKeySettings);
            for (var index = 0; index < _rules.Length; index++)
            {
                var rule = _rules[index];
                rule.Handle(dataset, context);
                _logger.LogDebug("Successfully handled rule {RuleIndex} ({RuleType}).", index, rule.GetType().Name);
            }
        }

        private List<(DicomDataset Dataset, AnonymizerRule Rule)> ValidateProcessing(DicomDataset dataset, RuntimeKeySettings runtimeKeySettings)
        {
            var exactRules = _rules.OfType<AnonymizerTagRule>().ToArray();
            var operations = new List<(DicomDataset Dataset, AnonymizerRule Rule)>();
            var visited = new HashSet<DicomDataset>(ReferenceEqualityComparer.Instance);
            var datasets = new Stack<(DicomDataset Dataset, bool Root)>();
            datasets.Push((dataset, true));
            while (datasets.Count > 0)
            {
                var current = datasets.Pop();
                if (!visited.Add(current.Dataset))
                {
                    continue;
                }

                var candidates = new HashSet<DicomItem>(
                    current.Dataset.Where(item => current.Root || exactRules.Any(exact =>
                        MatchesExactTag(current.Dataset, exact.Tag, item))),
                    ReferenceEqualityComparer.Instance);
                var visitedNodes = new HashSet<string>();
                var discardedSequences = new HashSet<DicomSequence>(ReferenceEqualityComparer.Instance);
                ProcessContext? context = null;
                foreach (var rule in _rules)
                {
                    if (candidates.Count == 0)
                    {
                        break;
                    }

                    IEnumerable<DicomItem> matches;
                    switch (rule)
                    {
                        case AnonymizerTagRule exact when rule.GetType() == typeof(AnonymizerTagRule):
                            matches = candidates.Where(item => MatchesExactTag(current.Dataset, exact.Tag, item));
                            break;
                        case AnonymizerMaskedTagRule masked when rule.GetType() == typeof(AnonymizerMaskedTagRule):
                            matches = candidates.Where(item => masked.MaskedTag.IsMatch(item.Tag));
                            break;
                        case AnonymizerVRRule vr when rule.GetType() == typeof(AnonymizerVRRule):
                            matches = candidates.Where(item => vr.VR == item.ValueRepresentation);
                            break;
                        default:
                            if (context == null)
                            {
                                context = InitContext(current.Dataset, runtimeKeySettings);
                                context.VisitedNodes = visitedNodes;
                            }

                            matches = rule.LocateDicomTag(current.Dataset, context).Where(candidates.Contains);
                            break;
                    }

                    var selectedItems = matches.ToArray();
                    foreach (var item in selectedItems)
                    {
                        rule.ValidateItem(item, !current.Root);
                        candidates.Remove(item);
                        visitedNodes.Add(item.ToString());
                        if (item is DicomSequence sequence && rule.DiscardsSequenceItems)
                        {
                            discardedSequences.Add(sequence);
                        }
                    }

                    if (!current.Root && selectedItems.Length > 0 && !rule.KeepsValue)
                    {
                        operations.Add((current.Dataset, rule));
                    }
                }

                foreach (var sequence in current.Dataset.OfType<DicomSequence>())
                {
                    if (!discardedSequences.Contains(sequence))
                    {
                        foreach (var nestedDataset in sequence.Items)
                        {
                            datasets.Push((nestedDataset, false));
                        }
                    }
                }
            }

            return operations;
        }

        private static bool MatchesExactTag(DicomDataset dataset, DicomTag tag, DicomItem candidate)
        {
            return dataset.Contains(tag) && ReferenceEquals(dataset.GetDicomItem<DicomItem>(tag), candidate);
        }

        private static void ValidateSequenceDepth(DicomDataset dataset)
        {
            var datasets = new Stack<(DicomDataset Dataset, int Depth)>();
            datasets.Push((dataset, 0));
            while (datasets.Count > 0)
            {
                var current = datasets.Pop();
                foreach (var sequence in current.Dataset.Where(item => item.ValueRepresentation == DicomVR.SQ).OfType<DicomSequence>())
                {
                    foreach (var nestedDataset in sequence.Items)
                    {
                        var depth = current.Depth + 1;
                        if (depth > MaximumSequenceDepth)
                        {
                            throw new AnonymizerOperationException(
                                DicomAnonymizationErrorCode.SequenceDepthLimitExceeded,
                                $"Nested sequence depth exceeds the supported limit of {MaximumSequenceDepth}.");
                        }

                        datasets.Push((nestedDataset, depth));
                    }
                }
            }
        }

        private static void ValidateFileMetaIdentity(DicomFile dicomFile)
        {
            var (datasetSopClass, datasetSopInstance) = GetRequiredDatasetIdentity(dicomFile.Dataset);
            if (dicomFile.FileMetaInfo == null ||
                !dicomFile.FileMetaInfo.TryGetSingleValue(DicomTag.MediaStorageSOPClassUID, out DicomUID mediaStorageSopClass) ||
                !dicomFile.FileMetaInfo.TryGetSingleValue(DicomTag.MediaStorageSOPInstanceUID, out DicomUID mediaStorageSopInstance) ||
                mediaStorageSopClass == null ||
                mediaStorageSopInstance == null ||
                mediaStorageSopClass != datasetSopClass ||
                mediaStorageSopInstance != datasetSopInstance)
            {
                throw CreateFileMetaIdentityMismatchException();
            }
        }

        private static (DicomUID SopClass, DicomUID SopInstance) GetRequiredDatasetIdentity(DicomDataset dataset)
        {
            if (!dataset.TryGetSingleValue(DicomTag.SOPClassUID, out DicomUID sopClass) ||
                !dataset.TryGetSingleValue(DicomTag.SOPInstanceUID, out DicomUID sopInstance) ||
                sopClass == null ||
                sopInstance == null)
            {
                throw CreateFileMetaIdentityMismatchException();
            }

            return (sopClass, sopInstance);
        }

        private static AnonymizerOperationException CreateFileMetaIdentityMismatchException()
        {
            return new AnonymizerOperationException(
                DicomAnonymizationErrorCode.FileMetaIdentityMismatch,
                "File Meta and Dataset SOP identities are missing or inconsistent.");
        }

        private static (bool UsesCryptoHash, bool UsesDateShift, bool UsesEncrypt) GetConfiguredKeyedMethods(JObject[] ruleContents)
        {
            var usesCryptoHash = false;
            var usesDateShift = false;
            var usesEncrypt = false;

            if (ruleContents == null)
            {
                return (usesCryptoHash, usesDateShift, usesEncrypt);
            }

            foreach (var ruleContent in ruleContents)
            {
                if (ruleContent == null || !ruleContent.TryGetValue(Constants.MethodKey, StringComparison.OrdinalIgnoreCase, out JToken methodToken))
                {
                    continue;
                }

                if (string.Equals(methodToken?.ToString(), nameof(AnonymizerMethod.CryptoHash), StringComparison.OrdinalIgnoreCase))
                {
                    usesCryptoHash = true;
                }
                else if (string.Equals(methodToken?.ToString(), nameof(AnonymizerMethod.DateShift), StringComparison.OrdinalIgnoreCase))
                {
                    usesDateShift = true;
                }
                else if (string.Equals(methodToken?.ToString(), nameof(AnonymizerMethod.Encrypt), StringComparison.OrdinalIgnoreCase))
                {
                    usesEncrypt = true;
                }

                if (usesCryptoHash && usesDateShift && usesEncrypt)
                {
                    break;
                }
            }

            return (usesCryptoHash, usesDateShift, usesEncrypt);
        }

        private void ValidateRequiredRuntimeKeys(RuntimeKeySettings runtimeKeySettings)
        {
            if (!_requireRuntimeKeys)
            {
                return;
            }

            if (_usesCryptoHash && string.IsNullOrWhiteSpace(runtimeKeySettings?.CryptoHashKey))
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Runtime CryptoHashKey is required when requireRuntimeKeys is enabled and a cryptoHash method is configured.");
            }

            if (_usesDateShift && string.IsNullOrWhiteSpace(runtimeKeySettings?.DateShiftKey))
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Runtime DateShiftKey is required when requireRuntimeKeys is enabled and a dateShift method is configured.");
            }

            if (_usesEncrypt && string.IsNullOrWhiteSpace(runtimeKeySettings?.EncryptKey))
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Runtime EncryptKey is required when requireRuntimeKeys is enabled and an encrypt method is configured.");
            }
        }
    }
}
