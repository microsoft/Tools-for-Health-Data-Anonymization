using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace Fhir.Anonymizer.Core.Resource
{
    public class ResourceIdTransformer
    {
        private const string InternalReferencePrefix = "#";
        private const string MappingFileDelimiter = "\t";
        private const string OidPrefix = "urn:oid:";
        private const string UuidPrefix = "urn:uuid:";
        // literal reference can be absolute or relative url, oid, or uuid.
        private static readonly List<Regex> _literalReferenceRegexes = new List<Regex>
        {
            new Regex(@"(Account|ActivityDefinition|AdverseEvent|AllergyIntolerance|Appointment|AppointmentResponse|AuditEvent|Basic|Binary|BiologicallyDerivedProduct|BodyStructure|Bundle|CapabilityStatement|CarePlan|CareTeam|CatalogEntry|ChargeItem|ChargeItemDefinition|Claim|ClaimResponse|ClinicalImpression|CodeSystem|Communication|CommunicationRequest|CompartmentDefinition|Composition|ConceptMap|Condition|Consent|Contract|Coverage|CoverageEligibilityRequest|CoverageEligibilityResponse|DetectedIssue|Device|DeviceDefinition|DeviceMetric|DeviceRequest|DeviceUseStatement|DiagnosticReport|DocumentManifest|DocumentReference|EffectEvidenceSynthesis|Encounter|Endpoint|EnrollmentRequest|EnrollmentResponse|EpisodeOfCare|EventDefinition|Evidence|EvidenceVariable|ExampleScenario|ExplanationOfBenefit|FamilyMemberHistory|Flag|Goal|GraphDefinition|Group|GuidanceResponse|HealthcareService|ImagingStudy|Immunization|ImmunizationEvaluation|ImmunizationRecommendation|ImplementationGuide|InsurancePlan|Invoice|Library|Linkage|List|Location|Measure|MeasureReport|Media|Medication|MedicationAdministration|MedicationDispense|MedicationKnowledge|MedicationRequest|MedicationStatement|MedicinalProduct|MedicinalProductAuthorization|MedicinalProductContraindication|MedicinalProductIndication|MedicinalProductIngredient|MedicinalProductInteraction|MedicinalProductManufactured|MedicinalProductPackaged|MedicinalProductPharmaceutical|MedicinalProductUndesirableEffect|MessageDefinition|MessageHeader|MolecularSequence|NamingSystem|NutritionOrder|Observation|ObservationDefinition|OperationDefinition|OperationOutcome|Organization|OrganizationAffiliation|Patient|PaymentNotice|PaymentReconciliation|Person|PlanDefinition|Practitioner|PractitionerRole|Procedure|Provenance|Questionnaire|QuestionnaireResponse|RelatedPerson|RequestGroup|ResearchDefinition|ResearchElementDefinition|ResearchStudy|ResearchSubject|RiskAssessment|RiskEvidenceSynthesis|Schedule|SearchParameter|ServiceRequest|Slot|Specimen|SpecimenDefinition|StructureDefinition|StructureMap|Subscription|Substance|SubstanceNucleicAcid|SubstancePolymer|SubstanceProtein|SubstanceReferenceInformation|SubstanceSourceMaterial|SubstanceSpecification|SupplyDelivery|SupplyRequest|Task|TerminologyCapabilities|TestReport|TestScript|ValueSet|VerificationResult|VisionPrescription)\/(?<id>[A-Za-z0-9\-\.]{1,64})"),
            new Regex(@"urn:oid:(?<id>[0-2](\.(0|[1-9][0-9]*))+)"),
            new Regex(@"urn:uuid:(?<id>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})")
        };

        private static readonly ConcurrentDictionary<string, string> _resourceIdMap = new ConcurrentDictionary<string, string>();
        private static readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();

        public static void Transform(ElementNode node)
        {
            if (ModelInfo.IsKnownResource(node.InstanceType))
            {
                var idNode = node.Children("id").Cast<ElementNode>().FirstOrDefault();
                if (idNode != null)
                {
                    var newId = TransformId(idNode.Value?.ToString());
                    _logger.LogDebug($"Resource Id {idNode.Value?.ToString()} is transformed to {newId}");
                    idNode.Value = newId;
                }
            }
            else if (ModelInfo.IsReference(node.InstanceType))
            {
                var referenceNode = node.Children("reference").Cast<ElementNode>().FirstOrDefault();
                if (referenceNode != null)
                {
                    var newReference = TransformIdFromReference(referenceNode.Value?.ToString());
                    referenceNode.Value = newReference;
                }
            }

            foreach(var child in node.Children().Cast<ElementNode>())
            {
                Transform(child);
            }
        }

        public static void SaveMappingFile(string mappingFile)
        {
            using var fileStream = new FileStream(mappingFile, FileMode.Create);
            using var writer = new StreamWriter(fileStream);
            foreach(var k in _resourceIdMap.Keys)
            {
                writer.WriteLine($"{k}{MappingFileDelimiter}{_resourceIdMap[k]}");
            }
        }

        public static void LoadMappingFile(string mappingFile)
        {
            using var fileStream = new FileStream(mappingFile, FileMode.Open);
            using var reader = new StreamReader(fileStream);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] idList = line.Split(MappingFileDelimiter);
                _resourceIdMap.TryAdd(idList[0], idList[1]);
            }
        }

        public static void LoadExistingMapping(Dictionary<string, string> mapping)
        {
            foreach(var entry in mapping)
            {
                _resourceIdMap.TryAdd(entry.Key, entry.Value);
            }
        }

        public static string TransformId(string id)
        {
            return string.IsNullOrEmpty(id) ? id : _resourceIdMap.GetOrAdd(id, Guid.NewGuid().ToString());
        }

        public static string TransformIdFromReference(string reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return reference;
            }

            if (reference.StartsWith(InternalReferencePrefix))
            {
                var internalId = reference.Substring(InternalReferencePrefix.Length);
                var newReference = $"{InternalReferencePrefix}{TransformId(internalId)}";
                _logger.LogDebug($"Internal reference {reference} is transformed to {newReference}.");

                return newReference;
            }

            foreach (var regex in _literalReferenceRegexes)
            {
                var match = regex.Match(reference);
                if (match.Success)
                {
                    var id = match.Groups["id"].Value;
                    var newReference = reference.Replace(id, TransformId(id));
                    if (newReference.StartsWith(OidPrefix))
                    {
                        newReference = newReference.Replace(OidPrefix, UuidPrefix);
                    }

                    _logger.LogDebug($"Literal reference {reference} is transformed to {newReference}.");
                    return newReference;
                }
            }

            return reference;
        }
    }
}
