using System;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Exceptions
{
    // Process runtime exception.
    public class AnonymizationProcessFailedException : Exception
    {
        public AnonymizationProcessFailedException(string message) : base(message)
        {
        }

        public AnonymizationProcessFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
