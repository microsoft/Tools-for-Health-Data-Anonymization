using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hl7.Fhir.Model;

namespace Fhir.Anonymizer.Core.Utility
{
    public class ReferenceUtility
    {
        private const string InternalReferencePrefix = "#";
        private static readonly List<Regex> _literalReferenceRegexes = new List<Regex>
        {
            // Regex for absolute or relative url reference, https://www.hl7.org/fhir/references.html#literal
            new Regex(@"^((http|https)://([A-Za-z0-9\\\/\.\:\%\$])*)?("
                + String.Join("|", ModelInfo.SupportedResources)
                + @")\/(?<id>[A-Za-z0-9\-\.]{1,64})(\/_history\/[A-Za-z0-9\-\.]{1,64})?$"),
            // Regex for oid reference https://www.hl7.org/fhir/datatypes.html#oid
            new Regex(@"^urn:oid:(?<id>[0-2](\.(0|[1-9][0-9]*))+)$"),
            // Regex for uuid reference https://www.hl7.org/fhir/datatypes.html#uuid
            new Regex(@"^urn:uuid:(?<id>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$")
        };

        public static string TransformReferenceId(string reference, Func<string, string> transformation)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return reference;
            }

            if (reference.StartsWith(InternalReferencePrefix))
            {
                var internalId = reference.Substring(InternalReferencePrefix.Length);
                var newReference = $"{InternalReferencePrefix}{transformation(internalId)}";

                return newReference;
            }

            foreach (var regex in _literalReferenceRegexes)
            {
                var match = regex.Match(reference);
                if (match.Success)
                {
                    var group = match.Groups["id"];
                    var newId = transformation(group.Value);
                    var newReference = $"{reference.Substring(0, group.Index)}{newId}";
                    // add reference suffix if exists (\/_history\/[A-Za-z0-9\-\.]{1,64})?
                    var suffixIndex = group.Index + group.Length;
                    newReference += reference.Substring(suffixIndex);

                    return newReference;
                }
            }

            // No id pattern found in reference, will hash whole reference value
            return transformation(reference);
        }
    }
}
