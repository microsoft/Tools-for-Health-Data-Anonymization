// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Health.Dicom.Anonymizer.Core;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Xunit;

namespace UnitTests.AnonymizationConfigurations
{
    public class AnonymizerConfigurationManagerTests
    {
        public AnonymizerConfigurationManagerTests()
        {
        }

        public static IEnumerable<object[]> GetInvalidConfigsForRuleParsing()
        {
            yield return new object[] { "./TestConfigurations/configuration-miss-tag.json" };
            yield return new object[] { "./TestConfigurations/configuration-unsupported-method.json" };
            yield return new object[] { "./TestConfigurations/configuration-invalid-DicomTag.json" };
        }

        public static IEnumerable<object[]> GetValidConfigs()
        {
            yield return new object[] { "./TestConfigurations/configuration-test-sample.json" };
        }

        [Fact]
        public void GivenANotExistConfig_WhenCreateAnonymizerConfigurationManager_ExceptionShouldBeThrown()
        {
            string configFilePath = "notExist";
            Assert.Throws<IOException>(() => AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath));
        }

        [Theory]
        [MemberData(nameof(GetInvalidConfigsForRuleParsing))]
        public void GivenAnInvalidConfigForRuleParsing_WhenCreateAnonymizerConfigurationManager_ExceptionShouldBeThrown(string configFilePath)
        {
            Assert.Throws<AnonymizationConfigurationException>(() => AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath));
        }

        [Theory]
        [MemberData(nameof(GetValidConfigs))]
        public void GivenAValidConfig_WhenCreateAnonymizerConfigurationManager_ConfigurationShouldBeLoaded(string configFilePath)
        {
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath);
            var dicomRules = configurationManager.DicomTagRules;
            Assert.True(dicomRules.Any());

            Assert.Single(configurationManager.DicomTagRules.Where(r => "(0008,0050)".Equals(r.Tag?.ToString())));
            Assert.Single(configurationManager.DicomTagRules.Where(r => "DA".Equals(r.VR?.ToString())));

            var settings = configurationManager.GetDefaultSettings();
            Assert.True(settings != null);
        }
    }
}
