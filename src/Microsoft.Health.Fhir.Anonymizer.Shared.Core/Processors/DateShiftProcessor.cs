using System.Collections.Generic;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
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
            var descendantsAndSelf = node.DescendantsAndSelf();

            foreach (var element in descendantsAndSelf)
            {
                if (element.Value == null || context?.VisitedNodes != null && context.VisitedNodes.Contains(element))
                {
                    continue;
                }

                var elementNode = (ElementNode) element;

                if (elementNode.IsDateNode())
                {
                    processResult.Update(DateTimeUtility.ShiftDateNode(elementNode, DateShiftKey, DateShiftKeyPrefix, EnablePartialDatesForRedact));
                }
                else if (elementNode.IsDateTimeNode() || elementNode.IsInstantNode())
                {
                    processResult.Update(DateTimeUtility.ShiftDateTimeAndInstantNode(elementNode, DateShiftKey, DateShiftKeyPrefix, EnablePartialDatesForRedact));
                }
                else
                {
                    throw new AnonymizerProcessingException(
                        $"DateShift is not applicable on node with type {elementNode.InstanceType}. Only FHIR date, dateTime and instant are applicable.");
                }
            }

            return processResult;
        }
    }
}
