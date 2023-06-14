// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Rules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.Rules
{
    public class AnonymizerRuleFactoryTests
    {
        private readonly AnonymizerConfiguration _configuration;

        private readonly AnonymizerRuleFactory _ruleFactory;

        public AnonymizerRuleFactoryTests()
        {
            var content = File.ReadAllText("Rules/settings.json");
            _configuration = JsonConvert.DeserializeObject<AnonymizerConfiguration>(content);
            _ruleFactory = new AnonymizerRuleFactory(_configuration, new DicomProcessorFactory());
        }

        public static IEnumerable<object[]> GetDicomTagRuleConfigsWithRedactMethod()
        {
            yield return new object[]
            {
                " { \"tag\": \"(0040,1001)\" , \"method\": \"redact\" } ",
                new DicomTag(0x0040, 0x1001),
                new RedactProcessor(JObject.Parse("{\"EnablePartialDatesForRedact\" : \"false\", \"EnablePartialAgesForRedact\" : \"false\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"00401001\" , \"method\": \"redact\", \"setting\":\"redactCustomerSetting\" } ",
                new DicomTag(0x0040, 0x1001),
                new RedactProcessor(JObject.Parse("{\"EnablePartialDatesForRedact\" : \"false\", \"EnablePartialAgesForRedact\" : \"true\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"00401001\" , \"method\": \"redact\", \"params\": {\"enablePartialDatesForRedact\" : true}, \"setting\":\"redactCustomerSetting\" } ",
                new DicomTag(0x0040, 0x1001),
                new RedactProcessor(JObject.Parse("{\"EnablePartialDatesForRedact\" : \"true\", \"EnablePartialAgesForRedact\" : \"true\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"0040,1001\" , \"method\": \"redact\", \"params\": {\"enablePartialDatesForRedact\" : true} } ",
                new DicomTag(0x0040, 0x1001),
                new RedactProcessor(JObject.Parse("{\"EnablePartialDatesForRedact\" : \"true\", \"EnablePartialAgesForRedact\" : \"false\"}")),
            };
        }

        public static IEnumerable<object[]> GetDicomTagRuleConfigsWithMethodsDoNotNeedSetting()
        {
            yield return new object[]
            {
                " { \"tag\": \"(0008,0001)\" , \"method\": \"remove\" } ",
                new DicomTag(0x0008, 0x0001),
                new RemoveProcessor(),
            };
            yield return new object[]
            {
                " { \"tag\": \"00101000\" , \"method\": \"keep\"} ",
                new DicomTag(0x0010, 0x1000),
                new KeepProcessor(),
            };
            yield return new object[]
            {
                " { \"tag\": \"00080008\" , \"method\": \"refreshuid\"} ",
                new DicomTag(0x0008, 0x0008),
                new RefreshUIDProcessor(),
            };
        }

        public static IEnumerable<object[]> GetDicomTagRuleConfigsWithPerturbMethod()
        {
            yield return new object[]
            {
                " { \"tag\": \"PatientName\" , \"method\": \"perturb\" } ",
                new DicomTag(0x0010, 0x0010),
                new PerturbProcessor(JObject.Parse("{\"span\": \"1\",\"roundTo\": \"2\", \"rangeType\": \"Proportional\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"SpecificCharacterSet\" , \"method\": \"perturb\" , \"params\": {\"Span\" : \"10\"} } ",
                new DicomTag(0x0008, 0x0005),
                new PerturbProcessor(JObject.Parse("{\"span\": \"10\",\"roundTo\": \"2\", \"rangeType\": \"Proportional\"}")),
            };
            yield return new object[]
            {
                "{ \"tag\": \"LanguageCodeSequence\" , \"method\": \"perturb\" , \"params\": {\"roundTo\" : \"3\"} , \"setting\": \"perturbCustomerSetting\" }",
                new DicomTag(0x0008, 0x0006),
                new PerturbProcessor(JObject.Parse("{\"span\": \"1\",\"roundTo\": \"3\", \"rangeType\": \"Fixed\"}")),
            };
            yield return new object[]
            {
                "{ \"tag\": \"LengthToEnd\" , \"method\": \"perturb\" , \"setting\": \"perturbCustomerSetting\" }",
                new DicomTag(0x0008, 0x0001),
                new PerturbProcessor(JObject.Parse("{\"span\": \"1\",\"roundTo\": \"2\", \"rangeType\": \"Fixed\"}")),
            };
        }

        public static IEnumerable<object[]> GetDicomVRRuleConfigsWithDateShiftMethod()
        {
            yield return new object[]
            {
                " { \"tag\": \"DA\" , \"method\": \"dateshift\", \"params\": {\"dateShiftScope\" : \"SOPInstance\"} }",
                DicomVR.DA,
                new DateShiftProcessor(JObject.Parse("{\"DateShiftKey\" : \"123\", \"DateShiftScope\" : \"SOPInstance\", \"DateShiftRange\" : \"50\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"DT\" , \"method\": \"dateshift\" } ",
                DicomVR.DT,
                new DateShiftProcessor(JObject.Parse("{\"DateShiftKey\" : \"123\", \"DateShiftScope\" : \"SeriesInstance\", \"DateShiftRange\" : \"50\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"CS\" , \"method\": \"dateshift\", \"setting\":\"dateShiftCustomerSetting\" } ",
                DicomVR.CS,
                new DateShiftProcessor(JObject.Parse("{\"DateShiftKey\" : \"123\", \"DateShiftScope\" : \"SOPInstance\", \"DateShiftRange\" : \"100\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"AS\" , \"method\": \"dateshift\", \"params\": {\"dateShiftScope\" : \"SeriesInstance\"}, \"setting\":\"dateShiftCustomerSetting\" } ",
                DicomVR.AS,
                new DateShiftProcessor(JObject.Parse("{\"DateShiftKey\" : \"123\", \"DateShiftScope\" : \"SeriesInstance\", \"DateShiftRange\" : \"100\"}")),
            };
        }

        public static IEnumerable<object[]> GetDicomVRRuleConfigsWithEncryptMethod()
        {
            yield return new object[]
            {
                " { \"tag\": \"DS\" , \"method\": \"encrypt\", \"params\": {\"encryptKey\" : \"0000000000000000\"} }",
                DicomVR.DS,
                new EncryptProcessor(JObject.Parse("{\"encryptKey\" : \"0000000000000000\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"AT\" , \"method\": \"encrypt\" } ",
                DicomVR.AT,
                new EncryptProcessor(JObject.Parse("{\"encryptKey\" : \"123456781234567812345678\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"FD\" , \"method\": \"encrypt\", \"setting\":\"encryptCustomerSetting\" } ",
                DicomVR.FD,
                new EncryptProcessor(JObject.Parse("{\"encryptKey\" : \"0000000000000000\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"IS\" , \"method\": \"encrypt\", \"params\": {\"encryptKey\" : \"1234567812345678\"}, \"setting\":\"encryptCustomerSetting\" } ",
                DicomVR.IS,
                new EncryptProcessor(JObject.Parse("{\"encryptKey\" : \"1234567812345678\"}")),
            };
        }

        public static IEnumerable<object[]> GetDicomMaskedTagRuleConfigsWithCryptoHashMethod()
        {
            yield return new object[]
            {
                " { \"tag\": \"(0040,xx01)\", \"method\": \"cryptohash\" }",
                DicomMaskedTag.Parse("(0040,xx01)"),
                new CryptoHashProcessor(JObject.Parse("{\"cryptoHashKey\": \"123\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"(000x,0001)\", \"method\": \"cryptohash\", \"setting\":\"cryptoHashCustomerSetting\" }",
                DicomMaskedTag.Parse("(000x,0001)"),
                new CryptoHashProcessor(JObject.Parse("{\"cryptoHashKey\": \"456\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"(0008,xxxx)\", \"method\": \"cryptohash\", \"params\": {\"cryptoHashKey\" : \"456\"}}",
                DicomMaskedTag.Parse("(0008,xxxx)"),
                new CryptoHashProcessor(JObject.Parse("{\"cryptoHashKey\": \"456\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"(0010,10xx)\", \"method\": \"cryptohash\", \"params\": {\"cryptoHashKey\" : \"123\"}, \"setting\":\"cryptoHashCustomerSetting\" }",
                DicomMaskedTag.Parse("(0010,10xx)"),
                new CryptoHashProcessor(JObject.Parse("{\"cryptoHashKey\": \"123\"}")),
            };
        }

        public static IEnumerable<object[]> GetDicomMaskedTagRuleConfigsWithSubstituteMethod()
        {
            yield return new object[]
            {
                " { \"tag\": \"(00x0,x001)\", \"method\": \"substitute\" }",
                DicomMaskedTag.Parse("(00x0,x001)"),
                new SubstituteProcessor(JObject.Parse("{\"replaceWith\" : \"ANONYMOUS\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"(xxxx,xxxx)\", \"method\": \"substitute\", \"setting\":\"substituteCustomerSetting\" }",
                DicomMaskedTag.Parse("(xxxx,xxxx)"),
                new SubstituteProcessor(JObject.Parse("{\"replaceWith\" : \"customized\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"(xxxx,0001)\", \"method\": \"substitute\", \"params\": {\"replaceWith\" : \"test\"}}",
                DicomMaskedTag.Parse("(xxxx,0001)"),
                new SubstituteProcessor(JObject.Parse("{\"replaceWith\": \"test\"}")),
            };
            yield return new object[]
            {
                " { \"tag\": \"(0xxx,10xx)\", \"method\": \"substitute\", \"params\": {\"replaceWith\" : \"test\"}, \"setting\":\"substituteCustomerSetting\" }",
                DicomMaskedTag.Parse("(0xxx,10xx)"),
                new SubstituteProcessor(JObject.Parse("{\"replaceWith\": \"test\"}")),
            };
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
        [MemberData(nameof(GetDicomTagRuleConfigsWithRedactMethod))]
        public void GivenADicomRuleWithRedact_WhenCreateAnonymizerRule_DicomRuleShouldBeCreateCorrectly(string config, DicomTag expectedTag, IAnonymizerProcessor expectedProcessor)
        {
            var rule = _ruleFactory.CreateDicomAnonymizationRule(JsonConvert.DeserializeObject<JObject>(config));
            var processor = typeof(AnonymizerRule).GetField("_processor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(rule);
            Assert.Equal(expectedTag, ((AnonymizerTagRule)rule).Tag);
            Assert.Equal(expectedProcessor.GetType(), processor.GetType());

            var expectedFunction = expectedProcessor.GetType().GetField("_redactFunction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(expectedProcessor);
            var expectedSetting = expectedFunction.GetType().GetField("_redactSetting", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(expectedFunction);
            var outputFunction = processor.GetType().GetField("_redactFunction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(processor);
            var outputSetting = outputFunction.GetType().GetField("_redactSetting", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(outputFunction);

            foreach (var prop in outputSetting.GetType().GetProperties())
            {
                Assert.Equal(expectedSetting.GetType().GetProperty(prop.Name).GetValue(expectedSetting), outputSetting.GetType().GetProperty(prop.Name).GetValue(outputSetting));
            }
        }

        [Theory]
        [MemberData(nameof(GetDicomTagRuleConfigsWithPerturbMethod))]
        public void GivenADicomRuleWithPerturb_WhenCreateAnonymizerRule_DicomRuleShouldBeCreateCorrectly(string config, DicomTag expectedTag, IAnonymizerProcessor expectedProcessor)
        {
            var rule = _ruleFactory.CreateDicomAnonymizationRule(JsonConvert.DeserializeObject<JObject>(config));
            var processor = typeof(AnonymizerRule).GetField("_processor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(rule);
            Assert.Equal(expectedTag, ((AnonymizerTagRule)rule).Tag);
            Assert.Equal(expectedProcessor.GetType(), processor.GetType());

            var expectedFunction = expectedProcessor.GetType().GetField("_perturbFunction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(expectedProcessor);
            var expectedSetting = expectedFunction.GetType().GetField("_perturbSetting", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(expectedFunction);
            var outputFunction = processor.GetType().GetField("_perturbFunction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(processor);
            var outputSetting = outputFunction.GetType().GetField("_perturbSetting", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(outputFunction);

            foreach (var prop in outputSetting.GetType().GetProperties())
            {
                Assert.Equal(expectedSetting.GetType().GetProperty(prop.Name).GetValue(expectedSetting), outputSetting.GetType().GetProperty(prop.Name).GetValue(outputSetting));
            }
        }

        [Theory]
        [MemberData(nameof(GetDicomTagRuleConfigsWithMethodsDoNotNeedSetting))]
        public void GivenADicomRuleWithMethodsDoNotNeedSetting_WhenCreateAnonymizerRule_DicomRuleShouldBeCreateCorrectly(string config, DicomTag expectedTag, IAnonymizerProcessor expectedProcessor)
        {
            var rule = _ruleFactory.CreateDicomAnonymizationRule(JsonConvert.DeserializeObject<JObject>(config));
            var processor = typeof(AnonymizerRule).GetField("_processor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(rule);
            Assert.Equal(expectedTag, ((AnonymizerTagRule)rule).Tag);
            Assert.Equal(expectedProcessor.GetType(), processor.GetType());
        }

        [Theory]
        [MemberData(nameof(GetDicomVRRuleConfigsWithDateShiftMethod))]
        public void GivenADicomVRRuleWithDateShift_WhenCreateAnonymizerRule_DicomVRRuleShouldBeCreateCorrectly(string config, DicomVR expectedVR, IAnonymizerProcessor expectedProcessor)
        {
            var rule = _ruleFactory.CreateDicomAnonymizationRule(JsonConvert.DeserializeObject<JObject>(config));
            var processor = typeof(AnonymizerRule).GetField("_processor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(rule);
            Assert.Equal(expectedVR, ((AnonymizerVRRule)rule).VR);
            Assert.Equal(expectedProcessor.GetType(), processor.GetType());

            var expectedFunction = expectedProcessor.GetType().GetField("_dateShiftFunction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(expectedProcessor);
            var expectedSetting = expectedFunction.GetType().GetField("_dateShiftSetting", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(expectedFunction);
            var outputFunction = processor.GetType().GetField("_dateShiftFunction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(processor);
            var outputSetting = outputFunction.GetType().GetField("_dateShiftSetting", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(outputFunction);

            foreach (var prop in outputSetting.GetType().GetProperties())
            {
                Assert.Equal(expectedSetting.GetType().GetProperty(prop.Name).GetValue(expectedSetting), outputSetting.GetType().GetProperty(prop.Name).GetValue(outputSetting));
            }
        }

        [Theory]
        [MemberData(nameof(GetDicomVRRuleConfigsWithEncryptMethod))]
        public void GivenADicomVRRuleWithEncryption_WhenCreateAnonymizerRule_DicomVRRuleShouldBeCreateCorrectly(string config, DicomVR expectedVR, IAnonymizerProcessor expectedProcessor)
        {
            var rule = _ruleFactory.CreateDicomAnonymizationRule(JsonConvert.DeserializeObject<JObject>(config));
            var processor = typeof(AnonymizerRule).GetField("_processor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(rule);
            Assert.Equal(expectedVR, ((AnonymizerVRRule)rule).VR);
            Assert.Equal(expectedProcessor.GetType(), processor.GetType());

            var expectedFunction = expectedProcessor.GetType().GetField("_encryptFunction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(expectedProcessor);
            var outputFunction = processor.GetType().GetField("_encryptFunction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(processor);

            foreach (var prop in outputFunction.GetType().GetProperties())
            {
                Assert.Equal(expectedFunction.GetType().GetProperty(prop.Name).GetValue(expectedFunction), outputFunction.GetType().GetProperty(prop.Name).GetValue(outputFunction));
            }
        }

        [Theory]
        [MemberData(nameof(GetDicomMaskedTagRuleConfigsWithCryptoHashMethod))]
        public void GivenADicomMaskedTagRuleWithCryptoHash_WhenCreateAnonymizerRule_DicomMaskedTagRuleShouldBeCreateCorrectly(string config, DicomMaskedTag expectedMaskedTag, IAnonymizerProcessor expectedProcessor)
        {
            var rule = _ruleFactory.CreateDicomAnonymizationRule(JsonConvert.DeserializeObject<JObject>(config));
            var processor = typeof(AnonymizerRule).GetField("_processor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(rule);
            Assert.Equal(expectedMaskedTag.ToString(), ((AnonymizerMaskedTagRule)rule).MaskedTag.ToString());
            Assert.Equal(expectedProcessor.GetType(), processor.GetType());

            var expectedFunction = expectedProcessor.GetType().GetField("_cryptoHashFunction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(expectedProcessor);
            var outputFunction = processor.GetType().GetField("_cryptoHashFunction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(processor);

            foreach (var prop in outputFunction.GetType().GetProperties())
            {
                Assert.Equal(expectedFunction.GetType().GetProperty(prop.Name).GetValue(expectedFunction), outputFunction.GetType().GetProperty(prop.Name).GetValue(outputFunction));
            }
        }

        [Theory]
        [MemberData(nameof(GetDicomMaskedTagRuleConfigsWithSubstituteMethod))]
        public void GivenADicomMaskedTagRuleWithSubstitution_WhenCreateAnonymizerRule_DicomMaskedTagRuleShouldBeCreateCorrectly(string config, DicomMaskedTag expectedMaskedTag, IAnonymizerProcessor expectedProcessor)
        {
            var rule = _ruleFactory.CreateDicomAnonymizationRule(JsonConvert.DeserializeObject<JObject>(config));
            var processor = typeof(AnonymizerRule).GetField("_processor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(rule);
            Assert.Equal(expectedMaskedTag.ToString(), ((AnonymizerMaskedTagRule)rule).MaskedTag.ToString());
            Assert.Equal(expectedProcessor.GetType(), processor.GetType());

            var expectedReplaceString = expectedProcessor.GetType().GetField("_replaceString", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(expectedProcessor);
            var outputReplaceString = processor.GetType().GetField("_replaceString", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(processor);

            Assert.Equal(expectedReplaceString, outputReplaceString);
        }

        [Theory]
        [MemberData(nameof(GetInvalidConfigs))]
        public void GivenAnInvalidDicomRule_WhenCreateDicomRule_ExceptionWillBeThrown(string config)
        {
            Assert.Throws<AnonymizerConfigurationException>(() => _ruleFactory.CreateDicomAnonymizationRule(JsonConvert.DeserializeObject<JObject>(config)));
        }
    }
}
