using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics.Html;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility.NerTAUtility;
using Newtonsoft.Json;
using Polly;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility.NerTAUtility
{
    public class TextAnalyticRecognizer : INamedEntityRecognizer
    {
        // Class members for HTTP requests
        private readonly ApiConfiguration _api;
        private readonly string _version = "v31preview1";
        private readonly int _maxLength = 5000; // byte
        private readonly int _maxRate = 200; // times/s
        private readonly HttpClient _client = new HttpClient();
        private static readonly int _maxNumberOfRetries = 6;
        protected static readonly HttpStatusCode[] _httpStatusCodesForRetrying = {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.TooManyRequests, // 429
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout // 504
        };
        // Class members for caching
        private readonly Cache _cache;
        // Class members for mapping
        private readonly MappingConfiguration _mapping;

        public TextAnalyticRecognizer(RecognizerConfiguration recognizerConfiguration)
        {
            _api = recognizerConfiguration.Api;
            _mapping = recognizerConfiguration.Mapper;
            _cache = new Cache(recognizerConfiguration.Cache, _version);
            // Configure client
            _client.BaseAddress = new Uri(new Uri(_api.Endpoint), _api.Path);
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _api.Key);
        }

        public List<Entity> ProcessSegment(Segment segment)
        {
            string responseString;
            try
            {
                responseString = _cache.Get(segment.DocumentId, segment.Offset);
            }
            catch
            {
                responseString = GetResponse(segment.Text).Result;
                _cache.Set(segment.DocumentId, segment.Offset, responseString);
            }
            var responseContent = JsonConvert.DeserializeObject<MicrosoftResponseContent>(responseString);
            var recognitionResult = ResponseContentToEntities(responseContent);
            return recognitionResult;
        }

        public int GetMaxLength()
        {
            return _maxLength;
        }

        public int GetMaxRate()
        {
            return _maxRate;
        }

        private HttpRequestMessage CreateRequestMessage(string requestText)
        {
            var microsoftRequestDocument = new MicrosoftRequestDocument()
            {
                DocumentId = "Microsoft.Ner",
                Language = "en",
                Text = requestText
            };
            var microsoftRequestContent = new MicrosoftRequestContent()
            {
                Documents = new List<MicrosoftRequestDocument>() { microsoftRequestDocument }
            };
            var content = new StringContent(JsonConvert.SerializeObject(microsoftRequestContent), Encoding.UTF8, "application/json");
            HttpRequestMessage requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = content
            };
            return requestMessage;
        }

        private async Task<string> GetResponse(string requestText)
        {
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => _httpStatusCodesForRetrying.Contains(r.StatusCode))
                .WaitAndRetryAsync(
                    _maxNumberOfRetries,
                    retryAttempt =>
                    {
                        Console.WriteLine("Processor: Retry");
                        return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    });

            var response = await retryPolicy.ExecuteAsync(
                    async ct => await _client.SendAsync(CreateRequestMessage(requestText), ct), CancellationToken.None);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }

        private List<Entity> ResponseContentToEntities(MicrosoftResponseContent responseContent)
        {
            var recognitionResult = new List<Entity>();
            if (responseContent.Documents.Count == 1)
            {
                var responseEntities = responseContent.Documents[0].Entities;
                foreach (var responseEntity in responseEntities)
                {
                    var entity = new Entity()
                    {
                        Category = Mapper.CategoryMapping(_mapping, responseEntity.Category, responseEntity.SubCategory),
                        SubCategory = Mapper.SubCategoryMapping(responseEntity.Category, responseEntity.SubCategory),
                        Text = responseEntity.Text,
                        Offset = responseEntity.Offset,
                        Length = responseEntity.Length,
                        ConfidenceScore = responseEntity.ConfidenceScore
                    };
                    if (entity.Category != string.Empty)
                    {
                        recognitionResult.Add(entity);
                    }
                }
            }
            return recognitionResult;
        }
    }
}
