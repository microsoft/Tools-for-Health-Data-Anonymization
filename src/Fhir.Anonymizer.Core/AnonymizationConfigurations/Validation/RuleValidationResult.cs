namespace Fhir.Anonymizer.Core.AnonymizerConfigurations.Validation
{
    public class RuleValidationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string TargetDataType { get; set; }
    }
}
