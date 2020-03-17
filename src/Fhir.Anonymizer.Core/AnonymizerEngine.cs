﻿using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Processors;
using Hl7.Fhir.ElementModel;
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

        public string AnonymizeJson(string json, bool isPrettyOutput = false)
        {
            EnsureArg.IsNotNullOrEmpty(json, nameof(json));

            ElementNode root;
            try
            {
                root = ElementNode.FromElement(_parser.Parse(json).ToTypedElement());
            }
            catch(Exception innerException)
            {
                throw new Exception("Failed to parse json resource, please check the json content.", innerException);
            }

            FhirJsonSerializationSettings settings = new FhirJsonSerializationSettings
            {
                Pretty = isPrettyOutput
            };
            return AnonymizeResourceNode(root).ToJson(settings);
        }

        public ElementNode AnonymizeResourceNode(ElementNode root)
        {
            EnsureArg.IsNotNull(root, nameof(root));

            if (root.IsBundleNode())
            {
                var entryResources = root.GetEntryResourceChildren();
                AnonymizeInternalResourceNodes(entryResources);
            }

            if (root.HasContainedNode())
            {
                var containedResources = root.GetContainedChildren();
                AnonymizeInternalResourceNodes(containedResources);
            }

            var resourceContext = ResourceAnonymizerContext.Create(root, _configurationManger);
            var resourceId = root.GetNodeId();
            foreach (var rule in resourceContext.RuleList)
            {
                var pathCompileExpression = new FhirPathCompiler().Compile($"{rule.Path}");
                var matchedNodes = pathCompileExpression(root, EvaluationContext.CreateDefault())
                    .Cast<ElementNode>();

                _logger.LogDebug(rule.Type == AnonymizerRuleType.PathRule ?
                    $"Path {rule.Source} matches {matchedNodes.Count()} nodes in resource ID {resourceId}." :
                    $"Type {rule.Source} matches {matchedNodes.Count()} nodes with path {rule.Path} in resource ID {resourceId}.");

                foreach (var node in matchedNodes)
                {
                    AnonymizeChildNode(node, rule, resourceContext.PathSet, resourceId);
                }
            }

            return root;
        }

        private void AnonymizeInternalResourceNodes(List<ElementNode> resourceNodes)
        {
            foreach(var resource in resourceNodes)
            {
                var newResource = AnonymizeResourceNode(GetResourceRoot(resource));
                resource.Parent.Replace(_provider, resource, newResource);
            }
        }

        private void AnonymizeChildNode(ElementNode node, AnonymizerRule rule, HashSet<string> rulePathSet, string resourceId)
        {
            var method = rule.Method.ToUpperInvariant();

            if (node.Value != null && _processors.ContainsKey(method))
            {
                _processors[method].Process(node);
                _logger.LogDebug($"{node.GetFhirPath()} in resource ID {resourceId} is applied {method} due to rule \"{rule.Source}:{rule.Method}\"");
            }

            var children = node.Children().Cast<ElementNode>();
            foreach (var child in children)
            {
                if (!rulePathSet.Contains(child.GetFhirPath()))
                {
                    AnonymizeChildNode(child, rule, rulePathSet, resourceId);
                }
            }
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
