﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnsureThat;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.Extensions;
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
        private readonly InternalAnonymizeLogic anonymizeLogic = null;

        public AnonymizerEngine(string configFilePath) : this(AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath)) 
        { 
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager)
        {
            _configurationManger = configurationManager;
            _processors = new Dictionary<string, IAnonymizerProcessor>();

            InitializeProcessors(_configurationManger);
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();

            anonymizeLogic = new InternalAnonymizeLogic(_configurationManger.FhirPathRules, _processors);

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

        public ElementNode AnonymizeResource(Resource resource, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNull(resource);

            ValidateInput(settings, resource);
            ElementNode root = ElementNode.FromElement(resource.ToTypedElement());
            ElementNode anonymizedNode = anonymizeLogic.Anonymize(root);
            ValidateOutput(settings, anonymizedNode);

            return anonymizedNode;
        }

        public string AnonymizeJson(string json, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNullOrEmpty(json, nameof(json));

            var resource = _parser.Parse<Resource>(json);
            ElementNode anonymizedNode = AnonymizeResource(resource, settings);

            FhirJsonSerializationSettings serializationSettings = new FhirJsonSerializationSettings
            {
                Pretty = settings != null && settings.IsPrettyOutput
            };
            return anonymizedNode.ToJson(serializationSettings);
        }

        private void ValidateInput(AnonymizerSettings settings, Resource resource)
        {
            if (settings != null && settings.ValidateInput)
            {
                _validator.ValidateInput(resource);
            }
        }

        private void ValidateOutput(AnonymizerSettings settings, ElementNode anonymizedNode)
        {
            if (settings != null && settings.ValidateOutput)
            {
                anonymizedNode.RemoveNullChildren();
                _validator.ValidateOutput(anonymizedNode.ToPoco<Resource>());
            }
        }

        private void InitializeProcessors(AnonymizerConfigurationManager configurationManager)
        {
            _processors[AnonymizerMethod.DateShift.ToString().ToUpperInvariant()] = DateShiftProcessor.Create(configurationManager);
            _processors[AnonymizerMethod.Redact.ToString().ToUpperInvariant()] = RedactProcessor.Create(configurationManager);
            _processors[AnonymizerMethod.Keep.ToString().ToUpperInvariant()] = new KeepProcessor();
        }
    }
}
