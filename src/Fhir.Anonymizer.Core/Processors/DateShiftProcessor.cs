using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Processors
{
    public class DateShiftProcessor : IAnonymizerProcessor
    {
        public string DateShiftKey { get; set; } = string.Empty;

        public bool EnablePartialDatesForRedact { get; set; } = false;

        public DateShiftProcessor(string dateShiftKey, bool enablePartialDatesForRedact)
        {
            this.DateShiftKey = dateShiftKey;
            this.EnablePartialDatesForRedact = enablePartialDatesForRedact;
        }

        public static DateShiftProcessor Create(AnonymizerConfigurationManager configuratonManager)
        {
            var parameters = configuratonManager.GetParameterConfiguration();
            return new DateShiftProcessor(parameters.DateShiftKey, parameters.EnablePartialDatesForRedact);
        }

        public void Process(ElementNode node)
        {
            if (node.IsDateNode())
            {
                DateTimeUtility.ShiftDateNode(node, DateShiftKey, EnablePartialDatesForRedact);
            }
            else if (node.IsDateTimeNode() || node.IsInstantNode())
            {
                DateTimeUtility.ShiftDateTimeAndInstantNode(node, DateShiftKey, EnablePartialDatesForRedact);
            }
        }
    }
}
