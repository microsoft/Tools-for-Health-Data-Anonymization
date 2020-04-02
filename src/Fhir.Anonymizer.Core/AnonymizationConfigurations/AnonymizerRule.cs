using System;
using System.Text.RegularExpressions;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class AnonymizerRule
    {
        private static Regex s_pathRegex = new Regex(@"^((?<resourceType>[A-Z][a-zA-Z]*)\.)?(?<expression>.+?)$");

        public string Path { get; set; }
        
        public string Method { get; set; }

        public string ResourceType { get; private set; }
        public string Expression { get; private set; }

        public AnonymizerRuleType Type { get; set; }

        public string Source { get; set; }

        public AnonymizerRule(string path, string method, AnonymizerRuleType type, string source)
        {
            Path = path;
            Method = method;
            Type = type;
            Source = source;

            InitTypeAndExpression(path);
        }

        public void InitTypeAndExpression(string path)
        {
            var match = s_pathRegex.Match(path);
            if (match.Success)
            {
                ResourceType = match.Groups["resourceType"].Value;
                Expression = match.Groups["expression"].Value;
            }
        }
    }
}
