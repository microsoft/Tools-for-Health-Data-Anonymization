using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core.Models.TextAnalytics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;

namespace Fhir.Anonymizer.Core.Utility.NamedEntityRecognition
{
    public class TextAnalyticRecognizer : INamedEntityRecognizer
    {
        private static readonly HttpClient _client = new HttpClient();
        private static readonly ILogger _logger = AnonymizerLogging.CreateLogger<TextAnalyticRecognizer>();
        private static readonly string _textAnalyticsApiKeyFieldName = "Ocp-Apim-Subscription-Key";
        private static readonly int _maxNumberOfRetries = 0;
        private static readonly HttpStatusCode[] _httpStatusCodesForRetrying = {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout // 504
        };
        private readonly string _apiEndpoint;

        public TextAnalyticRecognizer(string apiEndpoint, string apiKey)
        {
            _apiEndpoint = apiEndpoint;
            _client.DefaultRequestHeaders.Add(_textAnalyticsApiKeyFieldName, apiKey);
        }

        public async Task<IEnumerable<string>> AnonymizeText(IEnumerable<string> textList)
        {
            var resultList = textList.ToList();

            try
            {
                var documents = textList
                    .Select((text, id) => new TextAnalyticsRequestDocument
                    {
                        Id = id,
                        Language = "en",
                        Text = text
                    })
                    .Where(document => !string.IsNullOrWhiteSpace(document.Text));

                if (!documents.Any())
                {
                    return textList;
                }

                var requestContent = new TextAnalyticsRequestContent
                {
                    Documents = documents
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json");

                var response = await Policy
                    .Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(r => _httpStatusCodesForRetrying.Contains(r.StatusCode))
                    .WaitAndRetryAsync(
                        _maxNumberOfRetries,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    .ExecuteAsync(
                        async ct => await _client.PostAsync(_apiEndpoint, content, ct), CancellationToken.None);
                
                response.EnsureSuccessStatusCode();
                var responseData = await response.Content.ReadAsStringAsync();
                var responseContent = JsonConvert.DeserializeObject<TextAnalyticsResponseContent>(responseData);
                
                foreach (var document in responseContent.Documents)
                {
                    resultList[document.Id] = ProcessEntities(resultList[document.Id], document.Entities);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred in Text Analytics. Error message: {ex.ToString()}");
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
