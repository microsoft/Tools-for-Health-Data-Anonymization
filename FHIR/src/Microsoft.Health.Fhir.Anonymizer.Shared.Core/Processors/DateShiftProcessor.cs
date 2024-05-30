using System.Collections.Generic;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class DateShiftProcessor : IAnonymizerProcessor
    {
        public string DateShiftKey { get; set; } = string.Empty;

        public string DateShiftKeyPrefix { get; set; } = string.Empty;

        public int? DateShiftFixedOffsetInDays { get; set; } = null;

        public bool EnablePartialDatesForRedact { get; set; } = false;

        public DateShiftProcessor(string dateShiftKey, string dateShiftKeyPrefix, bool enablePartialDatesForRedact, int? dateShiftFixedOffsetInDays = null)
        {
            this.DateShiftKey = dateShiftKey;
            this.DateShiftKeyPrefix = dateShiftKeyPrefix;
            this.EnablePartialDatesForRedact = enablePartialDatesForRedact;
            this.DateShiftFixedOffsetInDays = dateShiftFixedOffsetInDays;
        }

        public static DateShiftProcessor Create(AnonymizerConfigurationManager configuratonManager)
        {
            var parameters = configuratonManager.GetParameterConfiguration();
            return new DateShiftProcessor(parameters.DateShiftKey, parameters.DateShiftKeyPrefix, parameters.EnablePartialDatesForRedact, parameters.DateShiftFixedOffsetInDays ?? null);
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }

            if (node.IsDateNode())
            {
                return DateTimeUtility.ShiftDateNode(node, DateShiftKey, DateShiftKeyPrefix, DateShiftFixedOffsetInDays, EnablePartialDatesForRedact);
            }
            else if (node.IsDateTimeNode() || node.IsInstantNode())
            {
                return DateTimeUtility.ShiftDateTimeAndInstantNode(node, DateShiftKey, DateShiftKeyPrefix, DateShiftFixedOffsetInDays, EnablePartialDatesForRedact);
            }

            return processResult;
        }
    }
}
