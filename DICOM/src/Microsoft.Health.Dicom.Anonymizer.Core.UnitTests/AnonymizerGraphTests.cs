// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class AnonymizerGraphTests
    {
        public static IEnumerable<object[]> GetCrossDepthCases()
        {
            foreach (var mode in new[] { "dataset", "inplace", "clone" })
            {
                foreach (var shallowFirst in new[] { false, true })
                {
                    yield return new object[] { mode, shallowFirst, 64 };
                    yield return new object[] { mode, shallowFirst, 65 };
                }
            }
        }

        public static IEnumerable<object[]> GetTopologyMutationCases()
        {
            foreach (var mode in new[] { "dataset", "inplace", "clone" })
            {
                foreach (var validateOutput in new[] { false, true })
                {
                    foreach (var topology in new[] { "self-cycle", "indirect-cycle", "amplified", "single", "shared" })
                    {
                        yield return new object[] { mode, validateOutput, topology };
                    }
                }
            }
        }

        [Fact]
        public void GivenHighlyAliasedGraph_WhenCheckingDepth_EqualDepthNodesAreEnumeratedOnce()
        {
            var budget = new EnumerationBudget(128);
            var dataset = CreateAliasedGraph(budget, 16);
            budget.Enabled = true;

            var validate = typeof(AnonymizerEngine).GetMethod("ValidateSequenceDepth", BindingFlags.Static | BindingFlags.NonPublic);
            validate.Invoke(null, new object[] { dataset });

            Assert.Equal(17, budget.Enumerations);
        }

        [Theory]
        [InlineData("dataset", false)]
        [InlineData("dataset", true)]
        [InlineData("inplace", false)]
        [InlineData("inplace", true)]
        [InlineData("clone", false)]
        [InlineData("clone", true)]
        public void GivenHighlyAliasedGraph_WhenAnonymizing_ExpansionFailsBeforeMutationOrRecursiveWork(string mode, bool validate)
        {
            var budget = new EnumerationBudget(256);
            var dataset = CreateAliasedGraph(budget, 16);
            var file = CreateFile(dataset);
            var buffer = new UnreadableBuffer();
            dataset.Add(new DicomOtherByte(DicomTag.PixelData, buffer));
            var engine = CreateEngine(validate);
            budget.Enabled = true;

            var error = Assert.Throws<AnonymizerOperationException>(() => Anonymize(engine, file, mode));

            Assert.Equal(DicomAnonymizationErrorCode.SequenceExpansionLimitExceeded, error.DicomAnonymizerErrorCode);
            Assert.Equal("ROOT", dataset.GetString(DicomTag.PatientName));
            Assert.Equal("2.25.100", dataset.GetString(DicomTag.SOPInstanceUID));
            Assert.Equal("2.25.100", file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            Assert.InRange(budget.Enumerations, 34, 256);
            Assert.Equal(0, budget.SequenceValidations);
            Assert.Equal(0, buffer.ReadAttempts);
            Assert.DoesNotContain("ROOT", error.ToString());
            Assert.DoesNotContain("2.25.100", error.ToString());
        }

        [Theory]
        [InlineData(65536)]
        [InlineData(65537)]
        public void GivenAliasExpansionAtBoundary_WhenAnalyzing_OnlyAdditionalOccurrencesAreLimited(int additionalOccurrences)
        {
            var shared = new DicomDataset();
            var sequence = new DicomSequence(DicomTag.ContentSequence);
            var dataset = new DicomDataset { sequence };
            for (var index = 0; index <= additionalOccurrences; index++)
            {
                sequence.Items.Add(shared);
            }

            var validate = typeof(AnonymizerEngine).GetMethod("ValidateDatasetStructure", BindingFlags.Static | BindingFlags.NonPublic);
            if (additionalOccurrences == 65536)
            {
                validate.Invoke(null, new object[] { dataset });
            }
            else
            {
                var exception = Assert.Throws<TargetInvocationException>(() => validate.Invoke(null, new object[] { dataset }));
                var error = Assert.IsType<AnonymizerOperationException>(exception.InnerException);
                Assert.Equal(DicomAnonymizationErrorCode.SequenceExpansionLimitExceeded, error.DicomAnonymizerErrorCode);
            }
        }

        [Fact]
        public void GivenLargeUnaliasedTree_WhenAnalyzing_AliasLimitDoesNotCapDistinctDatasets()
        {
            var sequence = new DicomSequence(DicomTag.ContentSequence);
            var dataset = new DicomDataset { sequence };
            for (var index = 0; index < 65537; index++)
            {
                sequence.Items.Add(new DicomDataset());
            }

            var validate = typeof(AnonymizerEngine).GetMethod("ValidateDatasetStructure", BindingFlags.Static | BindingFlags.NonPublic);
            validate.Invoke(null, new object[] { dataset });
        }

        [Fact]
        public void GivenGraphWithMoreThanInt64ExpandedOccurrences_WhenAnonymizing_CountSaturatesWithoutExpansion()
        {
            var budget = new EnumerationBudget(256);
            var dataset = CreateAliasedGraph(budget, 64);
            budget.Enabled = true;

            var error = Assert.Throws<AnonymizerOperationException>(() => CreateEngine(false).AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.SequenceExpansionLimitExceeded, error.DicomAnonymizerErrorCode);
            Assert.Equal(130, budget.Enumerations);
            Assert.Equal(0, budget.SequenceValidations);
        }

        [Theory]
        [MemberData(nameof(GetCrossDepthCases))]
        public void GivenSharedDatasetAtDifferentDepths_WhenAnonymizing_LongestPathDeterminesAcceptance(string mode, bool shallowFirst, int depth)
        {
            var leaf = new DicomDataset { { DicomTag.PatientID, "NESTED" } };
            var shared = new DicomDataset { new DicomSequence(DicomTag.ContentSequence, leaf) };
            var longerPath = shared;
            for (var index = 0; index < depth - 2; index++)
            {
                longerPath = new DicomDataset { new DicomSequence(DicomTag.ContentSequence, longerPath) };
            }

            var children = shallowFirst ? new[] { longerPath, shared } : new[] { shared, longerPath };
            var file = CreateFile(new DicomDataset { new DicomSequence(DicomTag.RequestAttributesSequence, children) });
            var engine = CreateEngine(false);

            if (depth == 65)
            {
                var error = Assert.Throws<AnonymizerOperationException>(() => Anonymize(engine, file, mode));

                Assert.Equal(DicomAnonymizationErrorCode.SequenceDepthLimitExceeded, error.DicomAnonymizerErrorCode);
                Assert.Equal("ROOT", file.Dataset.GetString(DicomTag.PatientName));
                Assert.Equal("NESTED", leaf.GetString(DicomTag.PatientID));
            }
            else
            {
                var output = Anonymize(engine, file, mode);

                Assert.False(output.Contains(DicomTag.PatientName));
                var direct = output.GetSequence(DicomTag.RequestAttributesSequence).Items[shallowFirst ? 1 : 0];
                var nested = Assert.Single(direct.GetSequence(DicomTag.ContentSequence).Items);
                Assert.Matches("^[0-9a-f]{64}$", nested.GetString(DicomTag.PatientID));
                Assert.Equal(mode == "clone", file.Dataset.Contains(DicomTag.PatientName));
            }

            Assert.Equal("2.25.100", file.Dataset.GetString(DicomTag.SOPInstanceUID));
            Assert.Equal("2.25.100", file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
        }

        [Theory]
        [InlineData("dataset")]
        [InlineData("inplace")]
        [InlineData("clone")]
        public void GivenOrdinarySharedDataset_WhenAnonymizingWithValidation_ValuesAreTransformedOnce(string mode)
        {
            var shared = new DicomDataset { { DicomTag.PatientID, "NESTED" } };
            var file = CreateFile(new DicomDataset { new DicomSequence(DicomTag.RequestAttributesSequence, shared, shared) });
            var expected = new DicomDataset { { DicomTag.PatientID, "NESTED" } };
            var engine = CreateEngine(true);
            engine.AnonymizeDataset(expected);

            var result = Anonymize(engine, file, mode);
            var children = result.GetSequence(DicomTag.RequestAttributesSequence).Items;

            Assert.Equal(2, children.Count);
            Assert.All(children, child => Assert.Equal(expected.GetString(DicomTag.PatientID), child.GetString(DicomTag.PatientID)));
            if (mode == "clone")
            {
                Assert.Equal("NESTED", shared.GetString(DicomTag.PatientID));
                Assert.NotSame(children[0], children[1]);
            }
            else
            {
                Assert.Same(shared, children[0]);
                Assert.Same(children[0], children[1]);
            }
        }

        [Theory]
        [MemberData(nameof(GetTopologyMutationCases))]
        public void GivenCustomProcessorChangesTopology_WhenAnonymizing_OutputStructureIsCheckedAgain(string mode, bool validateOutput, string topology)
        {
            var budget = new EnumerationBudget(1024) { Enabled = true };
            DicomDataset? mutated = null;
            var processor = new GraphMutationProcessor(dataset =>
            {
                mutated = dataset;
                var sequence = new CountingSequence(budget);
                dataset.Add(sequence);
                if (topology == "self-cycle")
                {
                    sequence.Items.Add(dataset);
                }
                else if (topology == "indirect-cycle")
                {
                    var nestedSequence = new CountingSequence(budget);
                    var child = new CountingDataset(budget) { nestedSequence };
                    sequence.Items.Add(child);
                    nestedSequence.Items.Add(dataset);
                }
                else if (topology == "amplified")
                {
                    sequence.Items.Add(CreateAliasedGraph(budget, 16));
                }
                else
                {
                    var child = new CountingDataset(budget) { { DicomTag.PatientID, "NEW" } };
                    sequence.Items.Add(child);
                    if (topology == "shared")
                    {
                        sequence.Items.Add(child);
                    }
                }
            });
            var engine = new AnonymizerEngine(
                AnonymizerConfigurationManager.CreateFromJson("{\"rules\":[{\"tag\":\"PatientName\",\"method\":\"graphMutation\"}]}"),
                new AnonymizerEngineOptions(false, validateOutput),
                processorFactory: new GraphProcessorFactory(processor));
            var file = CreateFile(new DicomDataset());

            if (topology == "single" || topology == "shared")
            {
                var result = Anonymize(engine, file, mode);
                var children = result.GetSequence(DicomTag.ContentSequence).Items;

                Assert.Equal(topology == "shared" ? 2 : 1, children.Count);
                Assert.All(children, child => Assert.Equal("NEW", child.GetString(DicomTag.PatientID)));
            }
            else
            {
                var error = Assert.Throws<AnonymizerOperationException>(() => Anonymize(engine, file, mode));

                Assert.Equal(
                    topology == "amplified" ? DicomAnonymizationErrorCode.SequenceExpansionLimitExceeded : DicomAnonymizationErrorCode.SequenceDepthLimitExceeded,
                    error.DicomAnonymizerErrorCode);
                Assert.Equal(0, budget.SequenceValidations);
            }

            Assert.NotNull(mutated);
            Assert.False(mutated.Contains(DicomTag.PatientName));
            Assert.Equal(mode == "clone", file.Dataset.Contains(DicomTag.PatientName));
            Assert.Equal(mode != "clone", file.Dataset.Contains(DicomTag.ContentSequence));
            Assert.Equal("2.25.100", file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
        }

        private static AnonymizerEngine CreateEngine(bool validate)
        {
            return new AnonymizerEngine(
                AnonymizerConfigurationManager.CreateFromJson(
                    "{\"rules\":[{\"tag\":\"PatientName\",\"method\":\"remove\"}," +
                    "{\"tag\":\"PatientID\",\"method\":\"cryptoHash\",\"params\":{\"cryptoHashKey\":\"synthetic-key\"}}]}"),
                new AnonymizerEngineOptions(validate, validate));
        }

        private static DicomFile CreateFile(DicomDataset dataset)
        {
            dataset.Add(DicomTag.SOPClassUID, DicomUID.CTImageStorage);
            dataset.Add(DicomTag.SOPInstanceUID, "2.25.100");
            dataset.Add(DicomTag.PatientName, "ROOT");
            return new DicomFile(dataset);
        }

        private static DicomDataset Anonymize(AnonymizerEngine engine, DicomFile file, string mode)
        {
            if (mode == "clone")
            {
                return engine.AnonymizeFile(file).Dataset;
            }

            if (mode == "inplace")
            {
                engine.AnonymizeFileInPlace(file);
            }
            else
            {
                engine.AnonymizeDataset(file.Dataset);
            }

            return file.Dataset;
        }

        private static DicomDataset CreateAliasedGraph(EnumerationBudget budget, int depth)
        {
            DicomDataset current = new CountingDataset(budget);
            for (var index = 0; index < depth; index++)
            {
                var sequence = new CountingSequence(budget);
                var parent = new CountingDataset(budget) { sequence };
                sequence.Items.Add(current);
                sequence.Items.Add(current);
                current = parent;
            }

            return current;
        }

        private sealed class EnumerationBudget
        {
            private readonly int _limit;

            public EnumerationBudget(int limit)
            {
                _limit = limit;
            }

            public bool Enabled { get; set; }

            public int Enumerations { get; private set; }

            public int SequenceValidations { get; private set; }

            public void Record()
            {
                if (Enabled && ++Enumerations > _limit)
                {
                    throw new InvalidOperationException("Graph enumeration exceeded the test operation budget.");
                }
            }

            public void ValidateSequence()
            {
                if (Enabled && ++SequenceValidations > _limit)
                {
                    throw new InvalidOperationException("Graph validation exceeded the test operation budget.");
                }
            }
        }

        private sealed class CountingDataset : DicomDataset, IEnumerable<DicomItem>
        {
            private readonly EnumerationBudget _budget;

            public CountingDataset(EnumerationBudget budget)
            {
                _budget = budget;
                DicomUtility.DisableAutoValidation(this);
            }

            IEnumerator<DicomItem> IEnumerable<DicomItem>.GetEnumerator()
            {
                _budget?.Record();
                return base.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<DicomItem>)this).GetEnumerator();
            }
        }

        private sealed class CountingSequence : DicomSequence
        {
            private readonly EnumerationBudget _budget;

            public CountingSequence(EnumerationBudget budget)
                : base(DicomTag.ContentSequence)
            {
                _budget = budget;
            }

            public override void Validate()
            {
                _budget?.ValidateSequence();
                base.Validate();
            }
        }

        private sealed class GraphProcessorFactory : DicomProcessorFactory
        {
            private readonly Dictionary<string, Type> _customProcessors = new Dictionary<string, Type>
            {
                ["graphMutation"] = typeof(GraphMutationProcessor),
            };

            private readonly IAnonymizerProcessor _processor;

            public GraphProcessorFactory(IAnonymizerProcessor processor)
            {
                _processor = processor;
            }

            public override IAnonymizerProcessor CreateProcessor(string method, JObject settingObject = null)
            {
                return method == "graphMutation" ? _processor : base.CreateProcessor(method, settingObject);
            }
        }

        private sealed class GraphMutationProcessor : IAnonymizerProcessor
        {
            private readonly Action<DicomDataset> _mutate;

            public GraphMutationProcessor(Action<DicomDataset> mutate)
            {
                _mutate = mutate;
            }

            public bool IsSupported(DicomItem item)
            {
                return item is DicomStringElement;
            }

            public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context)
            {
                dicomDataset.Remove(item.Tag);
                _mutate(dicomDataset);
            }
        }

        private sealed class UnreadableBuffer : IByteBuffer
        {
            public bool IsMemory => false;

            public long Size => 4;

            public int ReadAttempts { get; private set; }

            public byte[] Data
            {
                get
                {
                    ReadAttempts++;
                    throw new InvalidOperationException("Unexpected bulk buffer read.");
                }
            }

            public void GetByteRange(long offset, int count, byte[] output)
            {
                ReadAttempts++;
                throw new InvalidOperationException("Unexpected bulk buffer read.");
            }

            public void CopyToStream(Stream stream)
            {
                ReadAttempts++;
                throw new InvalidOperationException("Unexpected bulk buffer read.");
            }

            public Task CopyToStreamAsync(Stream stream, CancellationToken cancellationToken)
            {
                ReadAttempts++;
                throw new InvalidOperationException("Unexpected bulk buffer read.");
            }
        }
    }
}
