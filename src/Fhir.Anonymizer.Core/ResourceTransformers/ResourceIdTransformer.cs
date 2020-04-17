using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace Fhir.Anonymizer.Core.ResourceTransformers
{
    public class ResourceIdTransformer
    {
        private const string InternalReferencePrefix = "#";
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<ResourceIdTransformer>();

        // literal reference can be absolute or relative url, oid, or uuid.
        private readonly List<Regex> _literalReferenceRegexes = new List<Regex>
        {
            // Regex for absolute or relative url reference, https://www.hl7.org/fhir/references.html#literal
            new Regex(@"((http | https)://([A-Za-z0-9\\\/\.\:\%\$])*)?("
                + String.Join("|", ModelInfo.SupportedResources)
                + @")\/(?<id>[A-Za-z0-9\-\.]{1,64})(\/_history\/[A-Za-z0-9\-\.]{1,64})?"),
            // Regex for oid reference https://www.hl7.org/fhir/datatypes.html#oid
            new Regex(@"urn:oid:(?<id>[0-2](\.(0|[1-9][0-9]*))+)"),
            // Regex for uuid reference https://www.hl7.org/fhir/datatypes.html#uuid
            new Regex(@"urn:uuid:(?<id>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})")
        };

        public void Transform(ElementNode node)
        {
            if (node.IsResourceNode())
            {
                var idNode = node.Children("id").Cast<ElementNode>().FirstOrDefault();
                if (idNode != null)
                {
                    var id = idNode.Value?.ToString();
                    var newId = TransformResourceId(id);
                    _logger.LogDebug($"Resource Id {id} is transformed to {newId}");
                    idNode.Value = newId;
                }
            }
            else if (ModelInfo.IsReference(node.InstanceType))
            {
                var referenceNode = node.Children("reference").Cast<ElementNode>().FirstOrDefault();
                if (referenceNode != null)
                {
                    var reference = referenceNode.Value?.ToString();
                    var newReference = TransformIdFromReference(reference);
                    referenceNode.Value = newReference;
                }
            }

            foreach(var child in node.Children().Cast<ElementNode>())
            {
                Transform(child);
            }
        }

        public string TransformResourceId(string resourceId)
        {
            return HashUtility.GetResourceIdHash(resourceId);
        }

        public string TransformIdFromReference(string reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return reference;
            }

            if (reference.StartsWith(InternalReferencePrefix))
            {
                var internalId = reference.Substring(InternalReferencePrefix.Length);
                var newReference = $"{InternalReferencePrefix}{TransformResourceId(internalId)}";
                _logger.LogDebug($"Internal reference {reference} is transformed to {newReference}.");

                return newReference;
            }

            foreach (var regex in _literalReferenceRegexes)
            {
                var match = regex.Match(reference);
                if (match.Success)
                {
                    var group = match.Groups["id"];
                    var newId = TransformResourceId(group.Value);
                    var newReference = $"{reference.Substring(0, group.Index)}{newId}";
                    // add reference suffix if exists (\/_history\/[A-Za-z0-9\-\.]{1,64})?
                    var suffixIndex = group.Index + group.Length;
                    newReference += reference.Substring(suffixIndex);
                    _logger.LogDebug($"Literal reference {reference} is transformed to {newReference}.");

                    return newReference;
                }
            }

            return reference;
        }
    }
}
