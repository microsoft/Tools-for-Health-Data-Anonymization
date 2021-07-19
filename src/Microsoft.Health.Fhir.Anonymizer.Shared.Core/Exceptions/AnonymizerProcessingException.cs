using System;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Exceptions
{
    // Processing exception. A runtime exception thrown during anonymization process.
    // Customers can set the parameter in configuration file to skip processing the resource if this exception is thrown.
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
