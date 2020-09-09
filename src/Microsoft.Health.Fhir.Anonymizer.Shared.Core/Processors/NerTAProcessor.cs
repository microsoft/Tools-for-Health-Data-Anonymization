using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility.NerTAUtility;
using Newtonsoft.Json;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class NerTAProcessor : IAnonymizerProcessor
    {
        private INamedEntityRecognizer _namedEntityRecognizer { get; set; }
        private MultithreadingConfiguration multithreading { get; set; }

        public NerTAProcessor(ProcessorConfiguration processorConfiguration)
        {
            var type = processorConfiguration.recognizerConfiguration.Type;
            _namedEntityRecognizer = type switch
            {
                RecognizerType.MicrosoftNer => new TextAnalyticRecognizer(processorConfiguration.recognizerConfiguration),
                _ => throw new NotImplementedException($"The named entity recognition method is not supported: {type}"),
            };
            multithreading = processorConfiguration.Multithreading;
        }

        public static NerTAProcessor Create()
        {
            var configFilePath = "TA-config.json";
            var processorConfiguration = LoadConfig(configFilePath);
            return new NerTAProcessor(processorConfiguration);
        }

        private static ProcessorConfiguration LoadConfig(string configFilePath)
        {
            var content = File.ReadAllText(configFilePath);
            return JsonConvert.DeserializeObject<ProcessorConfiguration>(content);
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }
            // TODO: How to handle the documentId
            var documentId = "Patient";
            var originText = node.Value.ToString();

            // TODO: Whether to use textStripTags as the input of processor
            var originTextStripTags = HtmlTextUtility.StripTags(originText);
            // Console.WriteLine($"{originText.Length}, {originTextStripTags.Length}");

            var recognitionResults = GetRecognitionResults(documentId, originText);
            node.Value = ProcessEntities(originText, recognitionResults[documentId]);

            processResult.AddProcessRecord(AnonymizationOperations.Masked, node);
            return new ProcessResult();
        }

        public Dictionary<string, List<Entity>> GetRecognitionResults(string documentId, string text)
        {
            var segments = SegmentUtility.SegmentDocument(documentId, text, _namedEntityRecognizer.GetMaxLength());
            List<List<Entity>> segmentRecognitionResults;
            if (multithreading.Enable)
            {
                segmentRecognitionResults = MultithreadingProcess(segments, _namedEntityRecognizer, multithreading.Threads);
            }
            else
            {
                segmentRecognitionResults = SinglethreadingProcess(segments, _namedEntityRecognizer);
            }
            // Merge results
            var recognitionResults = SegmentUtility.MergeSegmentRecognitionResults(segments, segmentRecognitionResults);

            return recognitionResults;
        }

        public List<List<Entity>> MultithreadingProcess(List<Segment> segments, INamedEntityRecognizer processor, int threadNumber)
        {
            var segmentRecognitionResults = new List<Entity>[segments.Count];
            Parallel.For(
                0,
                segments.Count,
                new ParallelOptions { MaxDegreeOfParallelism = threadNumber },
                i => {
                    var segment = segments[i];
                    segmentRecognitionResults[i] = processor.ProcessSegment(segment);
                    Console.WriteLine("Finished: {0} {1}", segment.DocumentId, segment.Offset);
                }
            );
            return segmentRecognitionResults.OfType<List<Entity>>().ToList();
        }

        public List<List<Entity>> SinglethreadingProcess(List<Segment> segments, INamedEntityRecognizer processor)
        {
            var segmentRecognitionResults = new List<List<Entity>>();
            foreach (var segment in segments)
            {
                segmentRecognitionResults.Add(processor.ProcessSegment(segment));
                Console.WriteLine("Finished: {0} {1}", segment.DocumentId, segment.Offset);
            }
            return segmentRecognitionResults;
        }

        private string ProcessEntities(string originText, IEnumerable<Entity> textEntities)
        {
            if (string.IsNullOrWhiteSpace(originText))
            {
                return originText;
            }

            var result = new StringBuilder();
            // Use StringInfo to avoid offset issues https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/concepts/text-offsets
            var text = new StringInfo(originText);
            var startIndex = 0;
            foreach (var entity in textEntities)
            {
                Console.WriteLine($"{entity.Text}, [{entity.Category}]");
                result.Append(text.SubstringByTextElements(startIndex, entity.Offset - startIndex));
                result.Append($"[{entity.Category.ToUpperInvariant()}]");
                startIndex = entity.Offset + entity.Length;
            }
            if (startIndex < text.LengthInTextElements)
            {
                result.Append(text.SubstringByTextElements(startIndex));
            }

            return result.ToString();
        }
    }
}
