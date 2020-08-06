using System.Collections.Generic;
using MicrosoftFhir.Anonymizer.Core.Extensions;
using MicrosoftFhir.Anonymizer.Core.Models;
using MicrosoftFhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;

namespace MicrosoftFhir.Anonymizer.Core.Processors
{
    public class DateShiftProcessor : IAnonymizerProcessor
    {
        public string DateShiftKey { get; set; } = string.Empty;

        public string DateShiftKeyPrefix { get; set; } = string.Empty;

        public bool EnablePartialDatesForRedact { get; set; } = false;

        public DateShiftProcessor(string dateShiftKey, string dateShiftKeyPrefix, bool enablePartialDatesForRedact)
        {
            this.DateShiftKey = dateShiftKey;
            this.DateShiftKeyPrefix = dateShiftKeyPrefix;
            this.EnablePartialDatesForRedact = enablePartialDatesForRedact;
        }

        public static DateShiftProcessor Create(AnonymizerConfigurationManager configuratonManager)
        {
            var parameters = configuratonManager.GetParameterConfiguration();
            return new DateShiftProcessor(parameters.DateShiftKey, parameters.DateShiftKeyPrefix, parameters.EnablePartialDatesForRedact);
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
                return DateTimeUtility.ShiftDateNode(node, DateShiftKey, DateShiftKeyPrefix, EnablePartialDatesForRedact);
            }
            else if (node.IsDateTimeNode() || node.IsInstantNode())
            {
                return DateTimeUtility.ShiftDateTimeAndInstantNode(node, DateShiftKey, DateShiftKeyPrefix, EnablePartialDatesForRedact);
            }

            return processResult;
        }
    }
}
