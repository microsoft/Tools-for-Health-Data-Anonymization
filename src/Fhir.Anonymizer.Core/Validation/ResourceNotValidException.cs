using System;

namespace Fhir.Anonymizer.Core.Validation
{
    public class ResourceNotValidException : Exception
    {
        public ResourceNotValidException(string message) : base(message)
        {
        }
    }
}
