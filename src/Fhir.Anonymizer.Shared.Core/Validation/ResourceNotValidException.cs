using System;

namespace MicrosoftFhir.Anonymizer.Core.Validation
{
    public class ResourceNotValidException : Exception
    {
        public ResourceNotValidException(string message) : base(message)
        {
        }
    }
}
