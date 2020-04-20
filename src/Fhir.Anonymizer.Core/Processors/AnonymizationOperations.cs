﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Fhir.Anonymizer.Core.Processors
{
    public static class AnonymizationOperations
    {
        public const string Redact = "REDACT";
        public const string Abstract = "ABSTRACT";
        public const string Perturb = "PERTURB";
    }
}
