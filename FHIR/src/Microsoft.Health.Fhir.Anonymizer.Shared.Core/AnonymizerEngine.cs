using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.FhirPath;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Validation;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        private readonly AnonymizerConfigurationManager _configurationManager;
        private readonly Dictionary<string, IAnonymizerProcessor> _processors;
        private readonly AnonymizationFhirPathRule[] _rules;
        private readonly IStructureDefinitionSummaryProvider _provider = new PocoStructureDefinitionSummaryProvider();
        private readonly ResourceValidator _validator = new ResourceValidator();
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly IAnonymizerProcessorFactory _customProcessorFactory;

        public static void InitializeFhirPathExtensionSymbols()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        public AnonymizerEngine(string configFilePath, IAnonymizerProcessorFactory customProcessorFactory = null) : this(AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath), customProcessorFactory)
        {
            
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, IAnonymizerProcessorFactory customProcessorFactory = null)
        {
            _configurationManager = configurationManager;
            _processors = new Dictionary<string, IAnonymizerProcessor>();

            _customProcessorFactory = customProcessorFactory;

            InitializeProcessors(_configurationManager);

            _rules = _configurationManager.FhirPathRules;
            _logger.LogDebug("AnonymizerEngine initialized successfully");
        }

        public static AnonymizerEngine CreateWithFileContext(string configFilePath, string fileName, string inputFolderName, IAnonymizerProcessorFactory customProcessorFactory = null)
        {
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath);
            var dateShiftScope = configurationManager.GetParameterConfiguration().DateShiftScope;
            var dateShiftKeyPrefix = dateShiftScope switch
            {
                DateShiftScope.File => Path.GetFileName(fileName),
                DateShiftScope.Folder => Path.GetFileName(inputFolderName.TrimEnd('\\', '/')),
                _ => string.Empty
            };

            configurationManager.SetDateShiftKeyPrefix(dateShiftKeyPrefix);
            return new AnonymizerEngine(configurationManager, customProcessorFactory);
        }

        public ITypedElement AnonymizeElement(ITypedElement element, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            try
            {
                ElementNode resourceNode = ParseTypedElementToElementNode(element);
                return resourceNode.Anonymize(_rules, _processors);
            }
            catch (AnonymizerProcessingException)
            {
                if(_configurationManager.Configuration.processingErrors == ProcessingErrorsOption.Skip)
                {
                    // Return empty resource.
                    return new EmptyElement(element.InstanceType);
                }

                throw;
            }
        }

        public Resource AnonymizeResource(Resource resource, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));

            ValidateInput(settings, resource);
            var anonymizedResource = AnonymizeElement(resource.ToTypedElement()).ToPoco<Resource>();
            ValidateOutput(settings, anonymizedResource);
           
            return anonymizedResource;
        }

        public string AnonymizeJson(string json, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNullOrEmpty(json, nameof(json));

            var element = ParseJsonToTypedElement(json);
            var anonymizedElement = AnonymizeElement(element);

            var serializationSettings = new FhirJsonSerializationSettings
            {
                Pretty = settings != null && settings.IsPrettyOutput
            };

            return anonymizedElement.ToJson(serializationSettings);
        }

        private void ValidateInput(AnonymizerSettings settings, Resource resource)
        {
            if (settings != null && settings.ValidateInput)
            {
                _validator.ValidateInput(resource);
            }
        }

        private void ValidateOutput(AnonymizerSettings settings, Resource anonymizedNode)
        {
            if (settings != null && settings.ValidateOutput)
            {
                _validator.ValidateOutput(anonymizedNode);
            }
        }

        private void InitializeProcessors(AnonymizerConfigurationManager configurationManager)
        {
            _processors[AnonymizerMethod.DateShift.ToString().ToUpperInvariant()] = DateShiftProcessor.Create(configurationManager);
            _processors[AnonymizerMethod.Redact.ToString().ToUpperInvariant()] = RedactProcessor.Create(configurationManager);
            _processors[AnonymizerMethod.CryptoHash.ToString().ToUpperInvariant()] = new CryptoHashProcessor(configurationManager.GetParameterConfiguration().CryptoHashKey);
            _processors[AnonymizerMethod.Encrypt.ToString().ToUpperInvariant()] = new EncryptProcessor(configurationManager.GetParameterConfiguration().EncryptKey);
            _processors[AnonymizerMethod.Substitute.ToString().ToUpperInvariant()] = new SubstituteProcessor();
            _processors[AnonymizerMethod.Perturb.ToString().ToUpperInvariant()] = new PerturbProcessor();
            _processors[AnonymizerMethod.Keep.ToString().ToUpperInvariant()] = new KeepProcessor();
            _processors[AnonymizerMethod.Generalize.ToString().ToUpperInvariant()] = new GeneralizeProcessor();
            if (_customProcessorFactory != null)
            {
                InitializeCustomProcessors(configurationManager);
            }
        }

        private void InitializeCustomProcessors(AnonymizerConfigurationManager configurationManager)
        {
            var processors = _customProcessorFactory.GetType().GetField("_customProcessors", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_customProcessorFactory) as Dictionary<string, Type>;
            foreach(var processor in processors)
            {
                _processors[processor.Key.ToUpperInvariant()] = _customProcessorFactory.CreateProcessor(processor.Key, configurationManager.GetParameterConfiguration().CustomSettings);
            }
        }

        private ITypedElement ParseJsonToTypedElement(string json)
        {
            try
            {
                return FhirJsonNode.Parse(json).ToTypedElement(_provider);
            }
            catch (Exception ex)
            {
                throw new InvalidInputException($"The input FHIR resource JSON is invalid: {json}", ex);
            }
        }

        private static ElementNode ParseTypedElementToElementNode(ITypedElement element)
        {
            try
            {
                return ElementNode.FromElement(element);
            }
            catch (Exception ex)
            {
                throw new InvalidInputException("The input FHIR resource is invalid", ex);
            }
        }
    }
}
