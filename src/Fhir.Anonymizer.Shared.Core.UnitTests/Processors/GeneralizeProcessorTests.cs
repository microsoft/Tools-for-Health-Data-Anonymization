using System;
using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors;
using Fhir.Anonymizer.Core.Processors.Settings;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Model.Primitives;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Support.Model;
using Hl7.FhirPath;
using Newtonsoft.Json;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class GeneralizeTests
    {
        
        public static IEnumerable<object[]> GetEmptyNodestoGeneralize()
        {
            yield return new object[] { new Integer(), null };
            yield return new object[] { new UnsignedInt(), null };
            yield return new object[] { new PositiveInt(), null };
            yield return new object[] { new FhirString(), null };
            yield return new object[] { new Date(), null };
            yield return new object[] { new FhirDateTime(), null };
            yield return new object[] { new Time(), null };
        }

        public static IEnumerable<object[]> GetIntegerNodestoGeneralizeWithRangeMapping()
        {
            yield return new object[] { new Integer(5), 20 };
            yield return new object[] { new Integer(20), 40 };
            yield return new object[] { new Integer(43), 60 };
            yield return new object[] { new Integer(78), 80 };
            yield return new object[] { new Integer(110), null, "redact"};
            yield return new object[] { new Integer(110), 110, "keep" };
        }

        public static IEnumerable<object[]> GetIntegerNodestoGeneralizeWithApproximate()
        {
            yield return new object[] { new Integer(5), -10 };
            yield return new object[] { new Integer(20), 10 };
            yield return new object[] { new Integer(43), 30 };
            yield return new object[] { new Integer(78), 60 };
            yield return new object[] { new Integer(110), null, "redact" };
            yield return new object[] { new Integer(110), 110, "keep" };
            yield return new object[] { new PositiveInt(24), 10 };
            yield return new object[] { new PositiveInt(5), 1 };
            yield return new object[] { new UnsignedInt(24), 10 };
            yield return new object[] { new UnsignedInt(5), 0 };
        }

        public static IEnumerable<object[]> GetStringNodestoGeneralizeWithValueSet()
        {
            yield return new object[] { new FhirString("en-AU"), "en" };
            yield return new object[] { new FhirString("en-CA"), "en" };
            yield return new object[] { new FhirString("en-CI"), null, "redact" };
            yield return new object[] { new FhirString("es-AR"), "es" };
            yield return new object[] { new FhirString("es-ES"), "es" };
        }

        public static IEnumerable<object[]> GetStringNodestoGeneralizeWithMask()
        {
            yield return new object[] { new FhirString("1230005"), "123****" };
            yield return new object[] { new FhirString("1238765"), "123****" };
            yield return new object[] { new FhirString("2345234"), "234****" };
            yield return new object[] { new FhirString("1111111"), null, "redact" };
            yield return new object[] { new FhirString("7654321"), "7654321", "keep" };
        }

        public static IEnumerable<object[]> GetDateNodestoGeneralizeWithRangeMapping()
        {
            yield return new object[] { new Date("1990-01-01"), PartialDateTime.Parse("1990") };
            yield return new object[] { new Date("2000-01-01"), null, "redact" };
            yield return new object[] { new Date("1990"), PartialDateTime.Parse("1990"), "redact" };
            yield return new object[] { new Date("2000"), null, "redact" };
            yield return new object[] { new Date("2010"), PartialDateTime.Parse("2010-01-01"), "redact" };
            yield return new object[] { new Date("2010-01-01"), null, "redact" };
            yield return new object[] { new Date("2020"), PartialDateTime.Parse("2020-01-01") };
            yield return new object[] { new Date("2020-05-20"), PartialDateTime.Parse("2020-01-01") };
            yield return new object[] { new Date("2021-05-20"), PartialDateTime.Parse("2021-05-20"), "keep" };
        }

        public static IEnumerable<object[]> GetDateTimeNodestoGeneralizeWithRangeMapping()
        {
            yield return new object[] { new FhirDateTime("1990-01-01T00:00:00Z"), PartialDateTime.Parse("1990") };
            yield return new object[] { new FhirDateTime("1990-01-01T00:00:00"), null };
            yield return new object[] { new FhirDateTime("1990-01-01"), PartialDateTime.Parse("1990") };
            yield return new object[] { new FhirDateTime("1990-01"), PartialDateTime.Parse("1990") };
            yield return new object[] { new FhirDateTime("2000-01-01T00:00:00Z"), null };
            yield return new object[] { new FhirDateTime("2000-01-01T00:00:00"), PartialDateTime.Parse("1990") };
            yield return new object[] { new FhirDateTime("2000-01-01T00:00:00+09:00"), PartialDateTime.Parse("1990") };
            yield return new object[] { new FhirDateTime("2000-01-01T00:00:00-09:00"), null };
            yield return new object[] { new FhirDateTime("2010-01-01"), null };
            yield return new object[] { new FhirDateTime("2010-01-01T00:00:00Z"), null };
            yield return new object[] { new FhirDateTime("2010-01-01T00:00:00"), PartialDateTime.Parse("2010-01-01") };
            yield return new object[] { new FhirDateTime("2010-01-01T00:00:00+08:00"), PartialDateTime.Parse("2010-01-01") };
            yield return new object[] { new FhirDateTime("2009-12-31T16:00:00Z"), PartialDateTime.Parse("2010-01-01") };
            yield return new object[] { new FhirDateTime("2020-01-01T00:00:00"), PartialDateTime.Parse("2020-01-01") };
            yield return new object[] { new FhirDateTime("2020-01-01"), PartialDateTime.Parse("2020-01-01") };
        }

        public static IEnumerable<object[]> GetTimeNodestoGeneralizeWithRangeMapping()
        {
            yield return new object[] { new Time("13:45:02Z"), PartialTime.Parse("12:00:00+08:00") };
            yield return new object[] { new Time("13:45:02"), null };
            yield return new object[] { new Time("02:00"), null };
            yield return new object[] { new Time("02:00:00+08:00"), null };
            yield return new object[] { new Time("06:00:00-08:00"), PartialTime.Parse("12:00:00+08:00") };
            yield return new object[] { new Time("23:45:02"), PartialTime.Parse("12:00:00+08:00") };
            yield return new object[] { new Time("00:00:00+05:00"), PartialTime.Parse("00:00:00Z") };
            yield return new object[] { new Time("19:00:00Z"), null };
            yield return new object[] { new Time("03"), PartialTime.Parse("00:00:00Z") };
            yield return new object[] { new Time("00:00:00"),null };
            yield return new object[] { new Time("03:00:00"), PartialTime.Parse("00:00:00Z") };
            yield return new object[] { new Time("10"), PartialTime.Parse("10:00:00") };
            yield return new object[] { new Time("10:00:00"), PartialTime.Parse("10:00:00") };
            yield return new object[] { new Time("02:00:00Z"), PartialTime.Parse("10:00:00") };
            yield return new object[] { new Time("10"), PartialTime.Parse("10:00:00") };
        }

        public static IEnumerable<object[]> GetInstantNodestoGeneralizeWithRangeMapping()
        {
            yield return new object[] { new Instant(DateTimeOffset.Parse("2001-04-06T04:13:14Z")), PartialDateTime.Parse("1990-01-01T00:00:00Z") };
            yield return new object[] { new Instant(DateTimeOffset.Parse("1995-04-06T05:13:14+05:00")), PartialDateTime.Parse("1990-01-01T00:00:00Z") };
            yield return new object[] { new Instant(DateTimeOffset.Parse("2020-04-06T05:13:14+05:00")), null };

        }

        public static IEnumerable<object[]> GetDateNodestoGeneralizeWithOmitDay()
        {
            yield return new object[] { new Date("1990-11-01"), PartialDateTime.Parse("1990-11") };
            yield return new object[] { new Date("1990"), PartialDateTime.Parse("1990") };
            yield return new object[] { new Date("1990-11"), PartialDateTime.Parse("1990-11") };
        }

        public static IEnumerable<object[]> GetDateTimeNodestoGeneralizeWithOmitDay()
        {
            yield return new object[] { new FhirDateTime("1990-01-01T00:00:00Z"), PartialDateTime.Parse("1990-01") };
            yield return new object[] { new FhirDateTime("1990-01-01T00:00:00"), PartialDateTime.Parse("1990-01") };
            yield return new object[] { new FhirDateTime("1990-01-01"), PartialDateTime.Parse("1990-01") };
        }

        public static IEnumerable<object[]> GetInvalidTargetValuetoInteger()
        {
            yield return new object[] { new Integer(5), "{\"$this>=0 and $this<20\":\"'20'\"}" };
            yield return new object[] { new Integer(5), "{\"$this>=0 and $this<20\":\"2.5\"}" };
            yield return new object[] { new Integer(5), "{\"$this>=0 and $this<20\":\"@2015\"}" };
            yield return new object[] { new Integer(5), "{\"$this>=0 and $this<20\":\"@T00:00:00\"}" };
        }

        public static IEnumerable<object[]> GetInvalidCasesExpressions()
        {
            yield return new object[] { new Integer(5), "{\"$this>='0' and $this<'20'\":\"20\"}" };
            yield return new object[] { new Integer(5), "{\"$this>=0 and $this<20\":\"$this / 2\"}" };
            yield return new object[] { new Integer(5), "{\"$this>=0 && $this<20\":\"$this\"}" };
            yield return new object[] { new Integer(5), "{\"$this<5.5\":\"$this\"}" };
            yield return new object[] { new FhirString("en-AU"), "{\"$this>=0 and $this<20\":\"\"}" };
            yield return new object[] { new FhirDateTime("2015-01-01T00:00:00Z"), "{\"$this > @2015-1-1\":\"@2015-01-01\"}" };
            yield return new object[] { new FhirDateTime("2015-01-01T00:00:00Z"), "{\"$this > @2015-01-01T00:00\":\"@2015-01-01T00:00:00Z\"}" };
            yield return new object[] { new Integer(5), "{\"$this>=0 and $this<20\":\"\"}"};
            yield return new object[] { new Integer(25), "{\"$this>=0 and $this<20\":\"\"}" };
            yield return new object[] { new Integer(25), "{\"\":\"100\"}" };
            yield return new object[] { new Integer(5), "{\"\":\"\"}" };
        }

        public static IEnumerable<object[]> GetNodestoGeneralizeWithConflictSettings()
        {
            yield return new object[] { new Integer(18), 20 };
            yield return new object[] { new Integer(31), 40 };
            yield return new object[] { new Integer(45), 60 };
            yield return new object[] { new Integer(110), 80 };
        }

        private Dictionary<string, object> CreateRangeMapppingSettingsForInteger(string otherValues)
        {
            string Cases = "{\"$this>=0 and $this<20\":\"20\", \"$this>=20 and $this<40\":\"40\", \"$this>=40 and $this<60\":\"60\", \"$this>=60 and $this<80\":\"80\"}";
            string OtherValues = otherValues;
            return new Dictionary<string, object> { { "cases", Cases }, { "otherValues", OtherValues } };
        }

        private Dictionary<string, object> CreateApproximateSettingsForInteger(string otherValues)
        {
            string Cases = "{\"$this>=-20 and $this<80\":\"($this div 10 -1)*10 \"}";
            string OtherValues = otherValues;
            return new Dictionary<string, object> { { "cases", Cases }, { "otherValues", OtherValues } };
        }

        private Dictionary<string, object> CreateValueSetSettingsForString(string otherValues)
        {
            string Cases = "{\"$this in ('en-AU' | 'en-CA' | 'en-GB' | 'en-IN' | 'en-NZ' | 'en-SG' | 'en-US')\": \"'en'\",\"('es-AR' | 'es-ES' | 'es-UY') contains $this\": \"'es'\" }";
            string OtherValues = otherValues;
            return new Dictionary<string, object> { { "cases", Cases }, { "otherValues", OtherValues } };
        }

        private Dictionary<string, object> CreateMaskSettingsForString(string otherValues)
        {
            string Cases = "{\"$this.startsWith('123') or $this.endsWith('234')\": \"$this.substring(0,3)+'****'\" }";
            string OtherValues = otherValues;
            return new Dictionary<string, object> { { "cases", Cases }, { "otherValues", OtherValues } };
        }

        private Dictionary<string, object> CreateRangeMappingSettingsForDate(string otherValues)
        {
            string Cases = "{\"$this >= @1990-01-01 and $this < @2000-01-01\": \"@1990\", \"$this = @2010\" :\"@2010-01-01\",\"$this ~ @2020\":\"@2020-01-01\" }";
            string OtherValues = otherValues;
            return new Dictionary<string, object> { { "cases", Cases }, { "otherValues", OtherValues } };
        }

        private Dictionary<string, object> CreateRangeMappingSettingsForDateTime(string otherValues)
        {
            string Cases = "{\"$this >= @1990-01-01T00:00:00Z and $this <= @2000-01-01T00:00:00\": \"@1990\", \"$this = @2010-01-01T00:00:00\" :\"@2010-01-01\",\"$this ~ @2020-01-01T00:00:00\":\"@2020-01-01\" }";
            string OtherValues = otherValues;
            return new Dictionary<string, object> { { "cases", Cases }, { "otherValues", OtherValues } };
        }

        private Dictionary<string, object> CreateRangeMappingSettingsForTime(string otherValues)
        {
            string Cases = "{\"$this >= @T13:45:02Z and $this < @T23:45:02+05:00\": \"@T12:00:00+08:00\", \"$this = @T00:00:00+05:00\" :\"@T00:00:00Z\",\"$this ~ @T10:00:00\":\"@T10:00:00\" }";
            string OtherValues = otherValues;
            return new Dictionary<string, object> { { "cases", Cases }, { "otherValues", OtherValues } };
        }

        private Dictionary<string, object> CreateRangeMappingSettingsForInstant(string otherValues)
        {
            string Cases = "{\"$this >= @1990-01-01T00:00:00Z and $this <= @2020-01-01T00:00:00\": \"@1990-01-01T00:00:00Z\" }";
            string OtherValues = otherValues;
            return new Dictionary<string, object> { { "cases", Cases }, { "otherValues", OtherValues } };
        }

        private Dictionary<string, object> CreateOmitDaySettingsForDate(string otherValues)
        {
            string Cases = @"{""true"": ""$this.toString().replaceMatches('\\\\b(?<year>\\\\d{2,4})-(?<month>\\\\d{1,2})-(?<day>\\\\d{1,2})\\\\b','${year}-${month}')"" }";
            string OtherValues = otherValues;
            return new Dictionary<string, object> { { "cases", Cases }, { "otherValues", OtherValues } };
        }

        private Dictionary<string, object> CreateOmitDaySettingsForDateTime(string otherValues)
        {
            string Cases = @"{""true"": ""$this.toString().replaceMatches('\\\\b(?<year>\\\\d{2,4})-(?<month>\\\\d{1,2})-(?<day>\\\\d{1,2})(T)?(?<hour>\\\\d{1,2})?(:)?(?<minute>\\\\d{1,2})?(:)?(?<second>\\\\d{1,2})?(Z)?\\\\b','${year}-${month}')"" }";
            string OtherValues = otherValues;
            return new Dictionary<string, object> { { "cases", Cases }, { "otherValues", OtherValues } };
        }

        private Dictionary<string, object> CreateConflictSettings(string otherValues)
        {
            string Cases = "{\"$this>=0 and $this<20\":\"20\", \"$this>=10 and $this<40\":\"40\", \"$this>=30 and $this<60\":\"60\", \"true\":\"80\"}";
            string OtherValues = otherValues;
            return new Dictionary<string, object> { { "cases", Cases }, { "otherValues", OtherValues } };
        }

        [Theory]
        [MemberData(nameof(GetEmptyNodestoGeneralize))]
        public void GivenAnEmptyNode_WhenGeneralized_EmptyNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
         
            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateRangeMapppingSettingsForInteger(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.False(processResult.IsGeneralized);
            Assert.Equal(node.Value, target);
        }

        [Theory]
        [MemberData(nameof(GetIntegerNodestoGeneralizeWithRangeMapping))]
        public void GivenAnIntegerNode_WhenGeneralizedWithRangeMapping_GeneralizedNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
            
            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateRangeMapppingSettingsForInteger(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsGeneralized);
            Assert.Equal(node.Value, target);
        }

        [Theory]
        [MemberData(nameof(GetIntegerNodestoGeneralizeWithApproximate))]
        public void GivenAnIntegerNode_WhenGeneralizedWithApproximate_GeneralizedNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());

            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateApproximateSettingsForInteger(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsGeneralized);
            Assert.Equal(node.Value, target); ;
        }

        [Theory]
        [MemberData(nameof(GetStringNodestoGeneralizeWithValueSet))]
        public void GivenAStringNode_WhenGeneralizedWithValueSet_GeneralizedNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());

            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateValueSetSettingsForString(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsGeneralized);
            Assert.Equal(node.Value, target);
        }

        [Theory]
        [MemberData(nameof(GetStringNodestoGeneralizeWithMask))]
        public void GivenAStringNode_WhenGeneralizedWithMask_GeneralizedNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());

            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateMaskSettingsForString(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsGeneralized);
            Assert.Equal(node.Value, target);
        }

        [Theory]
        [MemberData(nameof(GetDateNodestoGeneralizeWithRangeMapping))]
        public void GivenADateNode_WhenGeneralizedWithRangeMapping_GeneralizedNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
           
            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateRangeMappingSettingsForDate(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsGeneralized);
            Assert.Equal(node.Value, target);
        }

        [Theory]
        [MemberData(nameof(GetDateTimeNodestoGeneralizeWithRangeMapping))]
        public void GivenADateTimeNode_WhenGeneralizedWithRangeMapping_GeneralizedNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());

            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateRangeMappingSettingsForDateTime(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsGeneralized);
            Assert.Equal(node.Value, target);
        }

        [Theory]
        [MemberData(nameof(GetTimeNodestoGeneralizeWithRangeMapping))]
        public void GivenATimeNode_WhenGeneralizedWithRangeMapping_GeneralizedNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());

            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateRangeMappingSettingsForTime(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsGeneralized);
            Assert.Equal(node.Value, target);
        }

        [Theory]
        [MemberData(nameof(GetInstantNodestoGeneralizeWithRangeMapping))]
        public void GivenAInstantNode_WhenGeneralizedWithRangeMapping_GeneralizedNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());

            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateRangeMappingSettingsForInstant(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsGeneralized);
            Assert.Equal(node.Value, target);
        }

        [Theory]
        [MemberData(nameof(GetDateNodestoGeneralizeWithOmitDay))]
        public void GivenADateNode_WhenGeneralizedWithOmitDay_GeneralizedNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());

            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateOmitDaySettingsForDate(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsGeneralized);
            Assert.Equal(node.Value, target);
        }

        [Theory]
        [MemberData(nameof(GetDateTimeNodestoGeneralizeWithOmitDay))]
        public void GivenADateTimeNode_WhenGeneralizedWithOmitDay_GeneralizedNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());

            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateOmitDaySettingsForDateTime(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsGeneralized);
            Assert.Equal(node.Value, target);
        }

        [Theory]
        [MemberData(nameof(GetInvalidTargetValuetoInteger))]
        public void GivenInvalidTargetValuetoInteger_WhenGeneralized_ExceptionShouldBeThrown(Base data, string cases)
        {
            var node = ElementNode.FromElement(data.ToTypedElement());

            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings= new Dictionary<string, object> { { "cases", cases } };

            Assert.Throws<AnonymizerConfigurationErrorsException>(() => processor.Process(node, context, settings));          
        }

        [Theory]
        [MemberData(nameof(GetInvalidCasesExpressions))]
        public void GivenInvalidCasesExpresions_WhenGeneralized_ExceptionShouldBeThrown(Base data, string cases)
        {
            var node = ElementNode.FromElement(data.ToTypedElement());

            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = new Dictionary<string, object> { { "cases", cases } };

            Assert.Throws<AnonymizerConfigurationErrorsException>(() => processor.Process(node, context, settings));
        }

        [Theory]
        [MemberData(nameof(GetNodestoGeneralizeWithConflictSettings))]
        public void GivenANode_WhenGeneralizedWithConflictSettings_GeneralizedNodeShouldBeReturned(Base data, object target, string otherValues = "redact")
        {
            var node = ElementNode.FromElement(data.ToTypedElement());

            GeneralizeProcessor processor = new GeneralizeProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = CreateConflictSettings(otherValues);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsGeneralized);
            Assert.Equal(node.Value, target);
        }
    }
}
