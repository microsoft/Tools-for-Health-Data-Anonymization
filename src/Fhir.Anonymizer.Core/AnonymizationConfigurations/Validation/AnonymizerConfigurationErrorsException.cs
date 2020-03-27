using System;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations.Validation
{
    public class AnonymizerConfigurationErrorsException : Exception
    {
        public AnonymizerConfigurationErrorsException(string message) : base(message)
        {
        }
    }
}
