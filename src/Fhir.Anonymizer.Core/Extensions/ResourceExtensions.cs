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
        public static void TryAddSecurityLabels(this Resource resource, ProcessResult result)
        {
            if (resource.Meta == null)
            {
                resource.Meta = new Meta();
            }

            if (result.IsRedacted && !resource.Meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.REDACT.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                resource.Meta.Security.Add(SecurityLabels.REDACT);
            }

            if (result.IsAbstracted && !resource.Meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.ABSTRED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                resource.Meta.Security.Add(SecurityLabels.ABSTRED);
            }
            
            if (result.IsPerturbed && !resource.Meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.PERTURBED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                resource.Meta.Security.Add(SecurityLabels.PERTURBED);
            }
        }
    }
}
