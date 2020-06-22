using Fhir.Anonymizer.Core.Models.TextAnalytics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core.Utility
{
    public class TextAnalyticUtility
    {
        private const string TextAnalyticsApiEndpoint = "https://fhiranalyser.cognitiveservices.azure.com/text/analytics/v3.1-preview.1/entities/recognition/pii?domain=phi&model-version=2020-04-01";
        private const string TextAnalyticsApiKeyValue = "[YOUR_COGNITIVE_SERVICE_KEY]";
        private const string TextAnalyticsApiKeyFieldName = "Ocp-Apim-Subscription-Key";

        private static readonly HttpClient _client = new HttpClient();

        static TextAnalyticUtility()
        {
            _client.BaseAddress = new Uri(TextAnalyticsApiEndpoint);
            _client.DefaultRequestHeaders.Add(TextAnalyticsApiKeyFieldName, TextAnalyticsApiKeyValue);
        }

        public async static Task<IEnumerable<string>> AnonymizeText(IEnumerable<string> textList)
        {
            var documents = textList.Select(
                (text, id) => new TextAnalyticsRequestDocument
                {
                    Id = id,
                    Language = "en",
                    Text = text
                }).Where(document => !string.IsNullOrWhiteSpace(document.Text));
            
            if (!documents.Any())
            {
                return textList;
            }

            var requestContent = new TextAnalyticsRequestContent
            {
                Documents = documents
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(TextAnalyticsApiEndpoint, content);
            response.EnsureSuccessStatusCode();
            var responseData = await response.Content.ReadAsStringAsync();
            var responseContent = JsonConvert.DeserializeObject<TextAnalyticsResponseContent>(responseData);

            var resultList = textList.ToList();
            foreach(var document in responseContent.Documents)
            {
                resultList[document.Id] = ProcessEntities(resultList[document.Id], document.Entities);
            }

            return resultList;
        }

        private static string ProcessEntities(string originText, IEnumerable<TextEntity> textEntities)
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
                result.Append(text.SubstringByTextElements(startIndex, entity.Offset - startIndex));
                result.Append($"[{entity.Category}]");
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
