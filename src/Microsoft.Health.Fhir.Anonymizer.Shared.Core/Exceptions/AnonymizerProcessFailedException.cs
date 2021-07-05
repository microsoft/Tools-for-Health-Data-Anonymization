using System;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Exceptions
{
    // Process runtime exception.
    public class AnonymizerProcessFailedException : Exception
    {
        public AnonymizerProcessFailedException(string message) : base(message)
        {
        }

        public AnonymizerProcessFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
