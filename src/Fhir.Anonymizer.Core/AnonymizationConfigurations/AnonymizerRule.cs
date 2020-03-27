namespace Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class AnonymizerRule
    {
        public string Path { get; set; }
        
        public string Method { get; set; }

        public AnonymizerRuleType Type { get; set; }

        public string Source { get; set; }
        // Rule priority, based on line sequence
        public int Priority { get; set; } 

        public AnonymizerRule(string path, string method, AnonymizerRuleType type, string source, int priority)
        {
            Path = path;
            Method = method;
            Type = type;
            Source = source;
            Priority = priority;
        }
    }
}
