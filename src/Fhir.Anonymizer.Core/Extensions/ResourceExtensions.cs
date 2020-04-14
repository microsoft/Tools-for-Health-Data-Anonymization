using Fhir.Anonymizer.Core.Models;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fhir.Anonymizer.Core.Extensions
{
    public static class ResourceExtensions
    {
        public static void TryAddSecurityLabels(this Resource resource, AnonymizationSummary summary)
        {
            if (resource.Meta == null)
            {
                resource.Meta = new Meta();
            }

            if (summary.IsRedacted && !resource.Meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.REDACT.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                resource.Meta.Security.Add(SecurityLabels.REDACT);
            }

            if (summary.IsAbstracted && !resource.Meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.ABSTRED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                resource.Meta.Security.Add(SecurityLabels.ABSTRED);
            }
            
            if (summary.IsPerturbed && !resource.Meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.PERTURBED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                resource.Meta.Security.Add(SecurityLabels.PERTURBED);
            }
        }
    }
}
