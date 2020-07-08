using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core.Models.DeepPavlov;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using DeepPavlovResponseDocument = System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<string>>;

namespace Fhir.Anonymizer.Core.Utility.NamedEntityRecognition
{
    public class DeepPavlovRecognizer : INamedEntityRecognizer
    {
        private static readonly HttpClient _client = new HttpClient();
        private static readonly ILogger _logger = AnonymizerLogging.CreateLogger<DeepPavlovRecognizer>();
        private static readonly string NonEntity = "O";
        private static readonly int _maxNumberOfRetries = 0;
        private static readonly HttpStatusCode[] _httpStatusCodesForRetrying = {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout // 504
        };
        private readonly string ApiEndpoint;

        public DeepPavlovRecognizer(string apiEndpoint)
        {
            ApiEndpoint = apiEndpoint;
        }

        public async Task<IEnumerable<string>> AnonymizeText(IEnumerable<string> textList)
        {
            var resultList = textList.ToList();

            try
            {
                var requestContent = new DeepPavlovRequestContent
                {
                    X = textList
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json");

                var response = await Policy
                    .Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(r => _httpStatusCodesForRetrying.Contains(r.StatusCode))
                    .WaitAndRetryAsync(
                        _maxNumberOfRetries,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    .ExecuteAsync(
                        async ct => await _client.PostAsync(ApiEndpoint, content, ct), CancellationToken.None); response.EnsureSuccessStatusCode();

                response.EnsureSuccessStatusCode();
                var responseData = await response.Content.ReadAsStringAsync();
                var responseContent = JsonConvert.DeserializeObject<IEnumerable<DeepPavlovResponseDocument>>(responseData).ToList();

                for (int i = 0; i < responseContent.Count; ++i)
                {
                    resultList[i] = ProcessEntities(resultList[i], responseContent[i]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred in DeepPavlov. Error message: {ex.ToString()}");
            }

            return resultList;
        }

        private static string ProcessEntities(string originText, DeepPavlovResponseDocument document)
        {
            if (string.IsNullOrWhiteSpace(originText))
            {
                return originText;
            }

            var originalTokens = document.First().ToList();
            var entityTokens = document.Last().ToList();
            var originalTokenIndexes = new int[originalTokens.Count];
            var startIndex = 0;
            for (int i = 0; i < originalTokens.Count; ++i)
            {
                originalTokenIndexes[i] = originText.IndexOf(originalTokens[i], startIndex);
                startIndex = originalTokenIndexes[i];
            }

            var result = new StringBuilder();
            var text = new StringInfo(originText);
            startIndex = 0;
            for (int i = 0; i < entityTokens.Count; ++i)
            {
                if (!string.Equals(entityTokens[i], NonEntity, StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Append(text.SubstringByTextElements(startIndex, originalTokenIndexes[i] - startIndex));
                    result.Append($"[{entityTokens[i].ToUpperInvariant()}]");
                    startIndex = originalTokenIndexes[i] + originalTokens[i].Length;
                }
            }
            if (startIndex < text.LengthInTextElements)
            {
                result.Append(text.SubstringByTextElements(startIndex));
            }

            return result.ToString();
        }
    }
}
