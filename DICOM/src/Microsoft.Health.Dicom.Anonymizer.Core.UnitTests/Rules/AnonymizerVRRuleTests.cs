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
    public class AnonymizerVRRuleTests
    {
        public AnonymizerVRRuleTests()
        {
            Dataset = new DicomDataset()
            {
                { DicomTag.PatientAge, "100Y" },  // AS
                { DicomTag.PatientName, "TestName" }, // PN
                { DicomTag.Referring​Physician​Name, "TestName2" }, // PN
            };
            VRRule = new AnonymizerVRRule(DicomVR.PN, "redact", "description", new DicomProcessorFactory(), JObject.Parse("{\"EnablePartialDatesForRedact\" : \"true\"}"));
        }

        public DicomDataset Dataset { get; set; }

        public AnonymizerVRRule VRRule { get; set; }

        [Fact]
        public void GivenAnonymizerVRRule_WhenHandleTheRule_TheLocatedItemWillBeProcessed()
        {
            var context = new ProcessContext();
            VRRule.Handle(Dataset, context);
            Assert.True(context.VisitedNodes.Count == 2);
            Assert.Empty(Dataset.GetString(DicomTag.PatientName));
            Assert.Empty(Dataset.GetString(DicomTag.Referring​Physician​Name));
        }

        [Fact]
        public void GivenAnonymizerVRRule_WhenHandleTheRule_IfRuleIsNotSupportedOnItem_ExceptionWillBeThrown()
        {
            var newRule = new AnonymizerVRRule(DicomVR.PN, "perturb", "description", new DicomProcessorFactory(), JObject.Parse("{}"));
            var context = new ProcessContext();
            Assert.Throws<AnonymizerOperationException>(() => newRule.Handle(Dataset, context));
        }

        [Fact]
        public void GivenAnonymizerVRRule_WhenLocateItem_AListOfResultWillBeReturned()
        {
            var result = VRRule.LocateDicomTag(Dataset, new ProcessContext());
            Assert.True(result.Count == 2);
            foreach (var item in result)
            {
                Assert.Equal(item.ValueRepresentation, VRRule.VR);
            }
        }

        [Fact]
        public void GivenAnonymizerVRRule_WhenLocateItem_VisitedItemsWillNotBeReturned()
        {
            var context = new ProcessContext();
            context.VisitedNodes.Add(Dataset.GetDicomItem<DicomItem>(DicomTag.PatientName).ToString());
            var result = VRRule.LocateDicomTag(Dataset, context);
            Assert.Single(result);
        }
    }
}
