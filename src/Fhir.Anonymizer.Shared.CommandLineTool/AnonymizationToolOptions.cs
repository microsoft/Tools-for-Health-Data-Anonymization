using System;
using System.Collections.Generic;
using System.Text;

namespace Fhir.Anonymizer.Tool
{
    public class AnonymizationToolOptions
    {
        public bool IsRecursive { get; set; }
        public bool ValidateInput { get; set; }
        public bool ValidateOutput { get; set; }
        public bool SkipExistedFile { get; set; }
    }
}
