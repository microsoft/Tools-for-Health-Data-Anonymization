namespace Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public enum AnonymizerMethod
    {
        Redact,
        DateShift,
        CryptoHash,
        Substitute,
        Keep
    }
}
