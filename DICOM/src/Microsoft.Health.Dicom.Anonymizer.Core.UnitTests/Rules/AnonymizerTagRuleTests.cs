// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Rules;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.Rules
{
    public class AnonymizerTagRuleTests
    {
        public AnonymizerTagRuleTests()
        {
            Dataset = new DicomDataset()
            {
                { DicomTag.PatientAge, "100Y" },  // AS
                { DicomTag.PatientName, "TestName" }, // CS
            };
            TagRule = new AnonymizerTagRule(DicomTag.PatientName, "redact", "description", new DicomProcessorFactory(), JObject.Parse("{\"EnablePartialDatesForRedact\" : \"true\"}"));
        }

        public DicomDataset Dataset { get; set; }

        public AnonymizerTagRule TagRule { get; set; }

        [Fact]
        public void GivenAnonymizerTagRule_WhenHandleTheRule_TheLocatedItemWillBeProcessed()
        {
            var context = new ProcessContext();
            TagRule.Handle(Dataset, context);
            Assert.Single(context.VisitedNodes);
            Assert.Empty(Dataset.GetString(DicomTag.PatientName));
        }

        [Fact]
        public void GivenAnonymizerTagRule_WhenHandleTheRule_IfRuleIsNotSupportedOnItem_ExceptionWillBeThrown()
        {
            var newRule = new AnonymizerTagRule(DicomTag.PatientName, "perturb", "description", new DicomProcessorFactory(), JObject.Parse("{}"));
            var context = new ProcessContext();
            Assert.Throws<AnonymizerOperationException>(() => newRule.Handle(Dataset, context));
        }

        [Fact]
        public void GivenAnonymizerTagRule_WhenLocateItem_AListOfResultWillBeReturned()
        {
            var context = new ProcessContext();
            var result = TagRule.LocateDicomTag(Dataset, context);
            Assert.Single(result);
            Assert.Equal(result[0].Tag, TagRule.Tag);
        }

        [Fact]
        public void GivenAnonymizerTagRule_WhenLocateItem_IfItemHasVisited_ResultListWillBeEmpty()
        {
            var context = new ProcessContext();
            context.VisitedNodes.Add(Dataset.GetDicomItem<DicomItem>(DicomTag.PatientName).ToString());
            var result = TagRule.LocateDicomTag(Dataset, context);
            Assert.Empty(result);
        }
    }
}
