using System;
using System.Collections.Generic;
using System.Text;

namespace Fhir.Anonymizer.Shared.Core.AnonymizerConfigurations
{
    public enum ProcessingErrorsOption
    {
        Raise, // Invalid processing will raise an exception.
        Skip,  // Invalid processing will return null.
        // Ignore Invalid processing will return input.
    }
}
