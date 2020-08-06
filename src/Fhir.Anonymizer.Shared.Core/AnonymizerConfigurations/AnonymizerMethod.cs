namespace MicrosoftFhir.Anonymizer.Core.AnonymizerConfigurations
{
    public enum AnonymizerMethod
    {
        Redact,
        DateShift,
        CryptoHash,
        Substitute,
        Encrypt,
        Perturb,
        Keep
    }
}
