using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnsureThat;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors;
using Fhir.Anonymizer.Core.Validation;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.FhirPath;
using Microsoft.Extensions.Logging;

namespace Fhir.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        private readonly FhirJsonParser _parser = new FhirJsonParser();
        private readonly PocoStructureDefinitionSummaryProvider _provider = new PocoStructureDefinitionSummaryProvider();
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly ResourceValidator _validator = new ResourceValidator();
        private readonly AnonymizerConfigurationManager _configurationManger;
        private readonly Dictionary<string, IAnonymizerProcessor> _processors;

        public AnonymizerEngine(string configFilePath) : this(AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath)) 
        { 
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager)
        {
            _configurationManger = configurationManager;
            _processors = new Dictionary<string, IAnonymizerProcessor>();
            InitializeProcessors(_configurationManger);
            _logger.LogDebug("AnonymizerEngine initialized successfully");
        }

        public static AnonymizerEngine CreateWithFileContext(string configFilePath, string fileName, string inputFolderName)
        {
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath);
            var dateShiftScope = configurationManager.GetParameterConfiguration().DateShiftScope;
            var dateShiftKeyPrefix = string.Empty;
            if (dateShiftScope == DateShiftScope.File)
            {
                dateShiftKeyPrefix = Path.GetFileName(fileName);
            }
            else if (dateShiftScope == DateShiftScope.Folder)
            {
                dateShiftKeyPrefix = Path.GetFileName(inputFolderName.TrimEnd('\\', '/'));
            }

            configurationManager.SetDateShiftKeyPrefix(dateShiftKeyPrefix);
            return new AnonymizerEngine(configurationManager);
        }

        public string AnonymizeJson(string json, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNullOrEmpty(json, nameof(json));

            ElementNode root;
            Resource resource;
            try
            {
                resource = _parser.Parse<Resource>(json);
                root = ElementNode.FromElement(resource.ToTypedElement());
            }
            catch(Exception innerException)
            {
                throw new Exception("Failed to parse json resource, please check the json content.", innerException);
            }

            if (settings != null && settings.ValidateInput)
            {
                _validator.ValidateInput(resource);
            }

            var processResult = AnonymizeResourceNode(root);
            root.RemoveNullChildren();
            var anonymizedResource = root.ToPoco<Resource>();
            anonymizedResource.TryAddSecurityLabels(processResult.Summary);

            if (settings != null && settings.ValidateOutput)
            {
                _validator.ValidateOutput(anonymizedResource);
            }
            
            FhirJsonSerializationSettings serializationSettings = new FhirJsonSerializationSettings
            {
                Pretty = settings != null && settings.IsPrettyOutput
            };
            return anonymizedResource.ToJson(serializationSettings);
        }

        public ProcessResult AnonymizeResourceNode(ElementNode root)
        {
            EnsureArg.IsNotNull(root, nameof(root));

            var processResult = new ProcessResult();
            if (root.IsBundleNode())
            {
                var entryResources = root.GetEntryResourceChildren();
                var entryResult = AnonymizeBundleEntryResourceNodes(entryResources);
                processResult.Summary.UpdateSummary(entryResult.Summary);
            }

            if (root.HasContainedNode())
            {
                var containedResources = root.GetContainedChildren();
                var containedResult = AnonymizeContainedResourceNodes(containedResources);
                processResult.Summary.UpdateSummary(containedResult.Summary);
            }

            var resourceContext = ResourceAnonymizerContext.Create(root, _configurationManger);
            var resourceId = root.GetNodeId();
            foreach (var rule in resourceContext.RuleList)
            {
                var matchedNodes = root.Select(rule.Path).Cast<ElementNode>();

                _logger.LogDebug(rule.Type == AnonymizerRuleType.PathRule ?
                    $"Path {rule.Source} matches {matchedNodes.Count()} nodes in resource ID {resourceId}." :
                    $"Type {rule.Source} matches {matchedNodes.Count()} nodes with path {rule.Path} in resource ID {resourceId}.");

                foreach (var node in matchedNodes)
                {
                    var result = AnonymizeChildNode(node, rule, resourceContext.PathSet, resourceId);
                    processResult.Summary.UpdateSummary(result.Summary);
                }
            }

            return processResult;
        }

        private ProcessResult AnonymizeContainedResourceNodes(List<ElementNode> resourceNodes)
        {
            var processResult = new ProcessResult();
            // For contained, all contained resources share the same status with root resource
            foreach (var resourceNode in resourceNodes)
            {
                var newResourceNode = GetResourceRoot(resourceNode);
                var result = AnonymizeResourceNode(newResourceNode);
                resourceNode.Parent.Replace(_provider, resourceNode, newResourceNode);
                processResult.Summary.UpdateSummary(result.Summary);
            }

            return processResult;
        }

        private ProcessResult AnonymizeBundleEntryResourceNodes(List<ElementNode> resourceNodes)
        {
            var processResult = new ProcessResult();
            // For Bundle, every entry has its own status
            foreach (var resourceNode in resourceNodes)
            {
                var newResourceNode = GetResourceRoot(resourceNode);
                var result = AnonymizeResourceNode(newResourceNode);
                
                newResourceNode.RemoveNullChildren();
                var anonymizedResource = newResourceNode.ToPoco<Resource>();
                anonymizedResource.TryAddSecurityLabels(result.Summary);
                
                resourceNode.Parent.Replace(_provider, resourceNode, ElementNode.FromElement(anonymizedResource.ToTypedElement()));
                processResult.Summary.UpdateSummary(result.Summary);
            }

            return processResult;
        }

        private ProcessResult AnonymizeChildNode(ElementNode node, AnonymizerRule rule, HashSet<string> rulePathSet, string resourceId)
        {
            var processResult = new ProcessResult();
            var method = rule.Method.ToUpperInvariant();

            if (node.Value != null && _processors.ContainsKey(method))
            {
                var result = _processors[method].Process(node);
                processResult.Summary.UpdateSummary(result.Summary);
                _logger.LogDebug($"{node.GetFhirPath()} in resource ID {resourceId} is applied {method} due to rule \"{rule.Source}:{rule.Method}\"");
            }

            var children = node.Children().Cast<ElementNode>();
            foreach (var child in children)
            {
                if (!rulePathSet.Contains(child.GetFhirPath()))
                {
                    var result = AnonymizeChildNode(child, rule, rulePathSet, resourceId);
                    processResult.Summary.UpdateSummary(result.Summary);
                }
            }

            return processResult;
        }

        private ElementNode GetResourceRoot(ElementNode node)
        {
            var content = node.ToJson();
            return ElementNode.FromElement(_parser.Parse(content).ToTypedElement());
        }

        private void InitializeProcessors(AnonymizerConfigurationManager configurationManager)
        {
            _processors[AnonymizerMethod.DateShift.ToString().ToUpperInvariant()] = DateShiftProcessor.Create(configurationManager);
            _processors[AnonymizerMethod.Redact.ToString().ToUpperInvariant()] = RedactProcessor.Create(configurationManager);
            _processors[AnonymizerMethod.Keep.ToString().ToUpperInvariant()] = new KeepProcessor();
        }
    }
}
