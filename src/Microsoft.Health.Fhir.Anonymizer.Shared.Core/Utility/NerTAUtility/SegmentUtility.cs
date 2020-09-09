using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility.NerTAUtility
{
    public class SegmentUtility
    {
        public static List<Segment> SegmentDocument(string documentId, string text, int maxSegmentLength)
        {
            var segments = new List<Segment>();
            int offset = 0;
            while (offset < text.Length)
            {
                string segmentText;
                if (text.Length - offset <= maxSegmentLength)
                {
                    segmentText = text.Substring(offset);
                }
                else
                {
                    segmentText = text.Substring(offset, maxSegmentLength);
                    var segmentLength = EndOfLastSentenceOrParagraph(segmentText);
                    if (segmentLength == 0)
                    {
                        segmentText = text.Substring(offset, segmentLength);
                    }
                }
                segments.Add(new Segment()
                {
                    DocumentId = documentId,
                    Text = segmentText,
                    Offset = offset
                });
                offset += segmentText.Length;
            }
            return segments;
        }

        public static int EndOfLastSentenceOrParagraph(string text)
        {
            return new int[] {
                text.LastIndexOf(". "), // end of last declarative sentence
                text.LastIndexOf("! "), // end of last exclamatory sentence
                text.LastIndexOf("? "), // end of last interrogative sentence
                text.LastIndexOf("\n")  // end of last paragraph
            }.Max() + 1;
        }

        public static Dictionary<string, List<Entity>> MergeSegmentRecognitionResults(List<Segment> segments, List<List<Entity>> segmentRecognitionResults)
        {
            var recognitionResults = new Dictionary<string, List<Entity>>();
            var entities = new List<Entity>();
            var documentId = segments.FirstOrDefault()?.DocumentId;
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].DocumentId != documentId)
                {
                    recognitionResults[documentId] = entities;
                    documentId = segments[i].DocumentId;
                    entities = new List<Entity>();
                }
                foreach (var entity in segmentRecognitionResults[i])
                {
                    entity.Offset += segments[i].Offset;
                    entities.Add(entity);
                }
            }
            recognitionResults[documentId] = entities;
            return recognitionResults;
        }
    }
}
