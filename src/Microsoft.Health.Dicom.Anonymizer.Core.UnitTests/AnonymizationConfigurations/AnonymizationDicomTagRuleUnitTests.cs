// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using Dicom;
using Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace UnitTests.AnonymizationConfigurations
{
    public class AnonymizationDicomTagRuleUnitTests
    {
        private AnonymizerConfiguration _configuration;

        public AnonymizationDicomTagRuleUnitTests()
        {
            var content = File.ReadAllText("AnonymizationConfigurations/settings.json");
            _configuration = JsonConvert.DeserializeObject<AnonymizerConfiguration>(content);
        }

        public static IEnumerable<object[]> GetDicomConfigs()
        {
            yield return new object[] { " { \"tag\": \"(0040,1001)\" , \"method\": \"redact\" } ", null, new DicomTag(0x0040, 0x1001), null, "redact", false, false, null };
            yield return new object[] { " { \"tag\": \"00401001\" , \"method\": \"redact\", \"setting\":\"redactCustomerSetting\" } ", null, new DicomTag(0x0040, 0x1001), null, "redact", false, false, new DicomRedactSetting { EnablePartialAgeForRedact = true, EnablePartialDatesForRedact = false } };
            yield return new object[] { " { \"tag\": \"00401001\" , \"method\": \"redact\", \"params\": {\"enablePartialDatesForRedact\" : true}, \"setting\":\"redactCustomerSetting\" } ", null, new DicomTag(0x0040, 0x1001), null, "redact", false, false, new DicomRedactSetting { EnablePartialAgeForRedact = true, EnablePartialDatesForRedact = true } };
            yield return new object[] { " { \"tag\": \"0040,1001\" , \"method\": \"redact\", \"params\": {\"enablePartialDatesForRedact\" : true} } ", null, new DicomTag(0x0040, 0x1001), null, "redact", false, false, new DicomRedactSetting { EnablePartialAgeForRedact = false, EnablePartialDatesForRedact = true } };

            yield return new object[] { " { \"tag\": \"PatientName\" , \"method\": \"perturb\" } ", null, new DicomTag(0x0010, 0x0010), null, "perturb", false, false, null };
            yield return new object[] { " { \"tag\": \"PatientName\" , \"method\": \"perturb\" , \"params\": {\"Span\" : \"10\"} } ", null, new DicomTag(0x0010, 0x0010), null, "perturb", false, false, new DicomPerturbSetting { Span = 10, RoundTo = 2, RangeType = PerturbRangeType.Proportional, Distribution = PerturbDistribution.Uniform } };
            yield return new object[] { "{ \"tag\": \"PatientName\" , \"method\": \"perturb\" , \"params\": {\"roundTo\" : \"3\"} , \"setting\": \"perturbCustomerSetting\" }", null, new DicomTag(0x0010, 0x0010), null, "perturb", false, false, new DicomPerturbSetting { Span = 1, RoundTo = 3, RangeType = PerturbRangeType.Fixed, Distribution = PerturbDistribution.Uniform } };
            yield return new object[] { "{ \"tag\": \"PatientName\" , \"method\": \"perturb\" , \"setting\": \"perturbCustomerSetting\" }", null, new DicomTag(0x0010, 0x0010), null, "perturb", false, false, new DicomPerturbSetting { Span = 1, RoundTo = 2, RangeType = PerturbRangeType.Fixed, Distribution = PerturbDistribution.Uniform } };

            yield return new object[] { " { \"tag\": \"DA\" , \"method\": \"dateshift\", \"params\": {\"dateShiftScope\" : \"SOPInstance\"} }", DicomVR.DA, null, null, "dateshift", true, false, new DicomDateShiftSetting { DateShiftKey = "123", DateShiftScope = DateShiftScope.SopInstance, DateShiftRange = 50 } };
            yield return new object[] { " { \"tag\": \"DA\" , \"method\": \"dateshift\" } ", DicomVR.DA, null, null, "dateshift", true, false, null };
            yield return new object[] { " { \"tag\": \"DA\" , \"method\": \"dateshift\", \"setting\":\"dateShiftCustomerSetting\" } ", DicomVR.DA, null, null, "dateshift", true, false, new DicomDateShiftSetting { DateShiftKey = "123", DateShiftScope = DateShiftScope.SopInstance, DateShiftRange = 100 } };
            yield return new object[] { " { \"tag\": \"DA\" , \"method\": \"dateshift\", \"params\": {\"dateShiftScope\" : \"SeriesInstance\"}, \"setting\":\"dateShiftCustomerSetting\" } ", DicomVR.DA, null, null, "dateshift", true, false, new DicomDateShiftSetting { DateShiftKey = "123", DateShiftScope = DateShiftScope.SeriesInstance, DateShiftRange = 100 } };

            yield return new object[] { " { \"tag\": \"(0040,xx01)\", \"method\": \"keep\" }", null, null, DicomMaskedTag.Parse("(0040,xx01)"), "keep", false, true, null };
            yield return new object[] { " { \"tag\": \"UI\", \"method\": \"refreshUID\" }", DicomVR.UI, null, null, "refreshUID", true, false, null };
            yield return new object[] { " { \"tag\": \"UI\", \"method\": \"remove\" }", DicomVR.UI, null, null, "remove", true, false, null };
        }

        public static IEnumerable<object[]> GetInvalidConfigs()
        {
            yield return new object[] { " { \"tag\": \"(0040)\" , \"method\": \"redact\" } " };
            yield return new object[] { " { \"tag\": \"DD\" , \"method\": \"dateshift\" } " };
            yield return new object[] { " { \"tag\": \"(0040, 0010)\" , \"method\": \"invalid\" } " };
            yield return new object[] { " { \"tag\": \"(0040, 0010)\" } " };
            yield return new object[] { " { \"method\": \"encrypt\" } " };
            yield return new object[] { " { \"tag\": \"PatientName\" ,  \"method\": \"perturb\" ,  \"setting\": \"CustomerSetting\" } " };
        }

        [Theory]
        [MemberData(nameof(GetDicomConfigs))]
        public void GivenADicomRule_WhenCreateDicomRule_DicomRuleShouldBeCreateCorrectly(string config, DicomVR vr, DicomTag tag, DicomMaskedTag maskedTag, string method, bool isVRRule, bool isMasked, IDicomAnonymizationSetting ruleSettings)
        {
            var rule = AnonymizerDicomTagRule.CreateAnonymizationDicomRule(JsonConvert.DeserializeObject<JObject>(config), _configuration);

            Assert.Equal(vr, rule.VR);
            Assert.Equal(tag, rule.Tag);
            Assert.Equal(maskedTag?.ToString(), rule.MaskedTag?.ToString());
            Assert.Equal(method, rule.Method);
            Assert.Equal(isVRRule, rule.IsVRRule);
            Assert.Equal(isMasked, rule.IsMasked);
            Assert.Equal(JsonConvert.SerializeObject(ruleSettings), JsonConvert.SerializeObject(rule.RuleSetting));
        }

        [Theory]
        [MemberData(nameof(GetInvalidConfigs))]
        public void GivenAnInvalidDicomRule_WhenCreateDicomRule_ExceptionWillBeThrown(string config)
        {
            Assert.Throws<AnonymizationConfigurationException>(() => AnonymizerDicomTagRule.CreateAnonymizationDicomRule(JsonConvert.DeserializeObject<JObject>(config), _configuration));
        }
    }
}
