using System;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Exceptions
{
    public class InvalidInputException : Exception
    {
        public InvalidInputException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
