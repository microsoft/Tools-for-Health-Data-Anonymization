using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    public enum GeneralizationOtherValuesOperation
    {
        Redact,
        Keep,
        CryptoHash,
        DateShift,
        Encrypt,
        Substitute,
        Perturb
    };
}