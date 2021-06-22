// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Rules;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.Rules
{
    public class AnonymizerMaskedTagRuleTests
    {
        public AnonymizerMaskedTagRuleTests()
        {
            Dataset = new DicomDataset()
            {
                { DicomTag.PatientAge, "100Y" },  // AS
                { DicomTag.Parse("0008,0010"), "test" }, // SH
                { DicomTag.Parse("0008,0012"), "20211110" }, // DA
                { DicomTag.Parse("0008,0013"), "121212.555" }, // TM
                { DicomTag.Parse("0008,0014"), "12345" }, // UI
            };

            MaskedTagRule = new AnonymizerMaskedTagRule(DicomMaskedTag.Parse("(0008,001x)"), "redact", "description", new DicomProcessorFactory(), JObject.Parse("{\"EnablePartialDatesForRedact\" : \"true\"}"));
        }

        public DicomDataset Dataset { get; set; }

        public AnonymizerMaskedTagRule MaskedTagRule { get; set; }

        [Fact]
        public void GivenAnonymizerMaskedTagRule_WhenHandleTheRule_TheLocatedItemWillBeProcessed()
        {
            var context = new ProcessContext();
            MaskedTagRule.Handle(Dataset, context);
            Assert.True(context.VisitedNodes.Count == 4);
            Assert.Empty(Dataset.GetString(DicomTag.Parse("0008,0010")));
            Assert.Equal("20210101", Dataset.GetString(DicomTag.Parse("0008,0012")));
            Assert.Empty(Dataset.GetString(DicomTag.Parse("0008,0013")));
            Assert.Empty(Dataset.GetString(DicomTag.Parse("0008,0014")));
        }

        [Fact]
        public void GivenAnonymizerMaskedTagRule_WhenLocateItem_AListOfResultWillBeReturned()
        {
            var result = MaskedTagRule.LocateDicomTag(Dataset, new ProcessContext());
            Assert.True(result.Count == 4);
            foreach (var item in result)
            {
                Assert.True(MaskedTagRule.MaskedTag.IsMatch(item.Tag));
            }
        }

        [Fact]
        public void GivenAnonymizerMaskedTagRule_WhenHandleTheRule_IfRuleIsNotSupportedOnItem_ExceptionWillBeThrown()
        {
            var newRule = new AnonymizerMaskedTagRule(DicomMaskedTag.Parse("(0008,001x)"), "perturb", "description", new DicomProcessorFactory(), JObject.Parse("{}"));
            var context = new ProcessContext();
            Assert.Throws<AnonymizerOperationException>(() => newRule.Handle(Dataset, context));
        }

        [Fact]
        public void GivenAnonymizerMaskedTagRule_WhenLocateItem_VisitedItemsWillNotBeReturned()
        {
            var context = new ProcessContext();
            context.VisitedNodes.Add(Dataset.GetDicomItem<DicomItem>(DicomTag.Parse("0008,0010")).ToString());
            context.VisitedNodes.Add(Dataset.GetDicomItem<DicomItem>(DicomTag.Parse("0008,0012")).ToString());
            var result = MaskedTagRule.LocateDicomTag(Dataset, context);
            Assert.True(result.Count == 2);
        }
    }
}
