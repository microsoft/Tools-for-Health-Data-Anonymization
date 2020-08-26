using System;
using System.Collections.Generic;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Model.Primitives;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class GeneralizeTests
    {
        public static IEnumerable<object[]> GetEmptyNodesToGeneralize()
        {
            yield return new object[] { new Integer(), null };
            yield return new object[] { new UnsignedInt(), null };
            yield return new object[] { new PositiveInt(), null };
            yield return new object[] { new FhirString(), null };
            yield return new object[] { new Date(), null };
            yield return new object[] { new FhirDateTime(), null };
            yield return new object[] { new Time(), null };
        }

        public static IEnumerable<object[]> GetIntegerNodesToGeneralizeWithRangeMapping()
        {
            yield return new object[] { new Integer(5), (long)20 };
            yield return new object[] { new Integer(20), (long)40 };
            yield return new object[] { new Integer(43), (long)60 };
            yield return new object[] { new Integer(78), (long)80 };
            yield return new object[] { new Integer(110), null, "Redact"};
            yield return new object[] { new Integer(110), 110, "Keep" };
        }

        public static IEnumerable<object[]> GetIntegerNodesToGeneralizeWithApproximate()
        {
            yield return new object[] { new Integer(5), (long)-10 };
            yield return new object[] { new Integer(20), (long)10 };
            yield return new object[] { new Integer(43), (long)30 };
            yield return new object[] { new Integer(78), (long)60 };
            yield return new object[] { new Integer(110), null, "Redact" };
            yield return new object[] { new Integer(110), 110, "Keep" };
            yield return new object[] { new PositiveInt(24), (long)10 };           
            yield return new object[] { new UnsignedInt(24), (long)10 };
        }

        public static IEnumerable<object[]> GetStringNodesToGeneralizeWithValueSet()
        {
            yield return new object[] { new FhirString("en-AU"), "en" };
            yield return new object[] { new FhirString("en-CA"), "en" };
            yield return new object[] { new FhirString("en-CI"), null, "Redact" };
            yield return new object[] { new FhirString("es-AR"), "es" };
            yield return new object[] { new FhirString("es-ES"), "es" };
        }

        public static IEnumerable<object[]> GetStringNodesToGeneralizeWithMask()
        {
            yield return new object[] { new FhirString("1230005"), "123****" };
            yield return new object[] { new FhirString("1238765"), "123****" };
            yield return new object[] { new FhirString("2345234"), "234****" };
            yield return new object[] { new FhirString("1111111"), null, "Redact" };
            yield return new object[] { new FhirString("7654321"), "7654321", "Keep" };
        }

        public static IEnumerable<object[]> GetDateNodesToGeneralizeWithRangeMapping()
        {
            yield return new object[] { new Date("1990-01-01"), "1990" };
            yield return new object[] { new Date("2000-01-01"), null, "Redact" };
            yield return new object[] { new Date("1990"), "1990", "Redact" };
            yield return new object[] { new Date("2000"), null, "Redact" };
            yield return new object[] { new Date("2010"), "2010-01-01", "Redact" };
            yield return new object[] { new Date("2010-01-01"), null, "Redact" };
            yield return new object[] { new Date("2020"), "2020-01-01" };
            yield return new object[] { new Date("2020-05-20"), "2020-01-01" };
            yield return new object[] { new Date("2021-05-20"), "2021-05-20", "Keep" };
        }

        public static IEnumerable<object[]> GetDateTimeNodesToGeneralizeWithRangeMapping()
        {
            yield return new object[] { new FhirDateTime("1990-01-01T00:00:00Z"), "1990" };
            yield return new object[] { new FhirDateTime("1990-01-01T00:00:00+08:00"), null };
            yield return new object[] { new FhirDateTime("1990-01-01"), "1990" };
            yield return new object[] { new FhirDateTime("1990-01"), "1990" };
            yield return new object[] { new FhirDateTime("2000-01-01T00:00:00Z"), null };
            yield return new object[] { new FhirDateTime("2000-01-01T00:00:00+08:00"), "1990" };
            yield return new object[] { new FhirDateTime("2000-01-01T00:00:00+09:00"), "1990" };
            yield return new object[] { new FhirDateTime("2000-01-01T00:00:00-09:00"), null };
            yield return new object[] { new FhirDateTime("2010-01-01"), null };
            yield return new object[] { new FhirDateTime("2010-01-01T00:00:00Z"), null };
            yield return new object[] { new FhirDateTime("2010-01-01T00:00:00+08:00"), "2010-01-01" };
            yield return new object[] { new FhirDateTime("2010-01-01T00:00:00+08:00"), "2010-01-01" };
            yield return new object[] { new FhirDateTime("2009-12-31T16:00:00Z"), "2010-01-01" };
            yield return new object[] { new FhirDateTime("2020-01-01T00:00:00+08:00"), "2020-01-01" };
            yield return new object[] { new FhirDateTime("2020-01-01"), "2020-01-01" };
        }

        public static IEnumerable<object[]> GetTimeNodesToGeneralizeWithRangeMapping()
        {
            yield return new object[] { new Time("13:45:02"), "12:00:00" };
            yield return new object[] { new Time("02:00:00"), null };
            yield return new object[] { new Time("06:00:00"), null };
            yield return new object[] { new Time("00:00:00"), "00:00:00" };
            yield return new object[] { new Time("10:00:00"), "10:00:00" };         
        }

        public static IEnumerable<object[]> GetInstantNodesToGeneralizeWithRangeMapping()
        {
            yield return new object[] { new Instant(DateTimeOffset.Parse("2001-04-06T04:13:14Z")), "1990-01-01T00:00:00Z" };
            yield return new object[] { new Instant(DateTimeOffset.Parse("1995-04-06T05:13:14+05:00")), "1990-01-01T00:00:00Z" };
            yield return new object[] { new Instant(DateTimeOffset.Parse("2020-04-06T05:13:14+05:00")), null };

        }

        public static IEnumerable<object[]> GetDateNodesToGeneralizeWithOmitDay()
        {
            yield return new object[] { new Date("1990-11-01"), "1990-11" };
            yield return new object[] { new Date("1990"), "1990" };
            yield return new object[] { new Date("1990-11"), "1990-11" };
        }

        public static IEnumerable<object[]> GetDateTimeNodesToGeneralizeWithOmitDay()
        {
            yield return new object[] { new FhirDateTime("1990-01-01T00:00:00Z"), "1990-01" };
            yield return new object[] { new FhirDateTime("1990-01-01T00:00:00"), "1990-01" };
            yield return new object[] { new FhirDateTime("1990-01-01"), "1990-01" };
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
            yield return new object[] { new Integer(25), "{\"\":\"100\"}" };
            yield return new object[] { new Integer(5), "{\"\":\"\"}" };
        }

        public static IEnumerable<object[]> GetNodesToGeneralizeWithConflictSettings()
        {
            yield return new object[] { new Integer(18), 20 };
            yield return new object[] { new Integer(31), 40 };
            yield return new object[] { new Integer(45), 60 };
            yield return new object[] { new Integer(110), 80 };
        }

        private Dictionary<string, object> CreateRangeMapppingSettingsForInteger(string otherValues)
        {
            string cases = "{\"$this>=0 and $this<20\":\"20\", \"$this>=20 and $this<40\":\"40\", \"$this>=40 and $this<60\":\"60\", \"$this>=60 and $this<80\":\"80\"}";
            return new Dictionary<string, object> { { "cases", cases }, { "otherValues", otherValues } };
        }

        private Dictionary<string, object> CreateApproximateSettingsForInteger(string otherValues)
        {
            string cases = "{\"$this>=-20 and $this<80\":\"($this div 10 -1)*10 \"}";
            return new Dictionary<string, object> { { "cases", cases }, { "otherValues", otherValues } };
        }

        private Dictionary<string, object> CreateValueSetSettingsForString(string otherValues)
        {
            string cases = "{\"$this in ('en-AU' | 'en-CA' | 'en-GB' | 'en-IN' | 'en-NZ' | 'en-SG' | 'en-US')\": \"'en'\",\"('es-AR' | 'es-ES' | 'es-UY') contains $this\": \"'es'\" }";
            return new Dictionary<string, object> { { "cases", cases }, { "otherValues", otherValues } };
        }

        private Dictionary<string, object> CreateMaskSettingsForString(string otherValues)
        {
            string cases = "{\"$this.startsWith('123') or $this.endsWith('234')\": \"$this.substring(0,3)+'****'\" }";
            return new Dictionary<string, object> { { "cases", cases }, { "otherValues", otherValues } };
        }

        private Dictionary<string, object> CreateRangeMappingSettingsForDate(string otherValues)
        {
            string cases = "{\"$this >= @1990-01-01 and $this < @2000-01-01\": \"@1990\", \"$this = @2010\" :\"@2010-01-01\",\"$this ~ @2020\":\"@2020-01-01\" }";
            return new Dictionary<string, object> { { "cases", cases }, { "otherValues", otherValues } };
        }

        private Dictionary<string, object> CreateRangeMappingSettingsForDateTime(string otherValues)
        {
            string cases = "{\"$this >= @1990-01-01T00:00:00Z and $this <= @2000-01-01T00:00:00+08:00\": \"@1990\", \"$this = @2010-01-01T00:00:00+08:00\" :\"@2010-01-01\",\"$this ~ @2020-01-01T00:00:00\":\"@2020-01-01\" }";
            return new Dictionary<string, object> { { "cases", cases }, { "otherValues", otherValues } };
        }

        private Dictionary<string, object> CreateRangeMappingSettingsForTime(string otherValues)
        {
            string cases = "{\"$this >= @T13:45:02 and $this < @T23:45:02\": \"@T12:00:00\", \"$this = @T00:00:00\" :\"@T00:00:00\",\"$this ~ @T10:00:00\":\"@T10:00:00\" }";
            return new Dictionary<string, object> { { "cases", cases }, { "otherValues", otherValues } };
        }

        private Dictionary<string, object> CreateRangeMappingSettingsForInstant(string otherValues)
        {
            string cases = "{\"$this >= @1990-01-01T00:00:00Z and $this <= @2020-01-01T00:00:00+08:00\": \"@1990-01-01T00:00:00Z\" }";
            return new Dictionary<string, object> { { "cases", cases }, { "otherValues", otherValues } };
        }

        private Dictionary<string, object> CreateOmitDaySettingsForDate(string otherValues)
        {
            string cases = @"{""true"": ""$this.toString().replaceMatches('\\\\b(?<year>\\\\d{2,4})-(?<month>\\\\d{1,2})-(?<day>\\\\d{1,2})\\\\b','${year}-${month}')"" }";
            return new Dictionary<string, object> { { "cases", cases }, { "otherValues", otherValues } };
        }

        private Dictionary<string, object> CreateOmitDaySettingsForDateTime(string otherValues)
        {
            string cases = @"{""true"": ""$this.toString().replaceMatches('\\\\b(?<year>\\\\d{2,4})-(?<month>\\\\d{1,2})-(?<day>\\\\d{1,2})(T)?(?<hour>\\\\d{1,2})?(:)?(?<minute>\\\\d{1,2})?(:)?(?<second>\\\\d{1,2})?(Z)?\\\\b','${year}-${month}')"" }";
            return new Dictionary<string, object> { { "cases", cases }, { "otherValues", otherValues } };
        }

        private Dictionary<string, object> CreateConflictSettings(string otherValues)
        {
            string cases = "{\"$this>=0 and $this<20\":\"20\", \"$this>=10 and $this<40\":\"40\", \"$this>=30 and $this<60\":\"60\", \"true\":\"80\"}";
            return new Dictionary<string, object> { { "cases", cases }, { "otherValues", otherValues } };
        }

        [Theory]
        [MemberData(nameof(GetEmptyNodesToGeneralize))]
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
            Assert.Equal(target, node.Value);
        }

        [Theory]
        [MemberData(nameof(GetIntegerNodesToGeneralizeWithRangeMapping))]
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
            Assert.Equal(target, node.Value);
        }

        [Theory]
        [MemberData(nameof(GetIntegerNodesToGeneralizeWithApproximate))]
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
            Assert.Equal(target, node.Value);
        }

        [Theory]
        [MemberData(nameof(GetStringNodesToGeneralizeWithValueSet))]
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
            Assert.Equal(target, node.Value);
        }

        [Theory]
        [MemberData(nameof(GetStringNodesToGeneralizeWithMask))]
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
            Assert.Equal(target, node.Value);
        }

        [Theory]
        [MemberData(nameof(GetDateNodesToGeneralizeWithRangeMapping))]
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
            Assert.Equal(target, node.Value?.ToString());
        }

        [Theory]
        [MemberData(nameof(GetDateTimeNodesToGeneralizeWithRangeMapping))]
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
            Assert.Equal(target, node.Value?.ToString());
        }

        [Theory]
        [MemberData(nameof(GetTimeNodesToGeneralizeWithRangeMapping))]
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
            Assert.Equal(target, node.Value?.ToString());
        }

        [Theory]
        [MemberData(nameof(GetInstantNodesToGeneralizeWithRangeMapping))]
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
            Assert.Equal(target, node.Value?.ToString());
        }

        [Theory]
        [MemberData(nameof(GetDateNodesToGeneralizeWithOmitDay))]
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
            Assert.Equal(target, node.Value?.ToString());
        }

        [Theory]
        [MemberData(nameof(GetDateTimeNodesToGeneralizeWithOmitDay))]
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
            Assert.Equal(target, node.Value?.ToString());
        }

        [Theory]
        [MemberData(nameof(GetInvalidCasesExpressions))]
        public void GivenInvalidCasesExpressions_WhenGeneralized_ExceptionShouldBeThrown(Base data, string cases)
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
        [MemberData(nameof(GetNodesToGeneralizeWithConflictSettings))]
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
            Assert.Equal(target, Convert.ToInt32(node.Value));
        }
    }
}
