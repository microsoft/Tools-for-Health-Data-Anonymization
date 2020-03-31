using System;
using System.Collections.Generic;
using System.Text;

namespace Fhir.Anonymizer.Core.AnonymizationConfigurations
{
    public class AnonymizationExtendTypeRule
    {
        public string Path { get; set; }

        public int Priority { get; set; }

        public string Action { get; set; }
    }
}
