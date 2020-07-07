using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Fhir.Anonymizer.Core.Utility.NamedEntityRecognition
{
    public class NamedEntityRecognitionSharedUtility
    {
        public static readonly int MaxNumberOfRetries = 0;
        public static readonly HttpStatusCode[] HttpStatusCodesForRetrying = {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout // 504
        };

        public static async Task<HttpResponseMessage> Request(HttpClient client, HttpContent content, string apiEndpoint)
        {
            return await Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => HttpStatusCodesForRetrying.Contains(r.StatusCode))
                .WaitAndRetryAsync(
                    MaxNumberOfRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                .ExecuteAsync(
                    async ct => await client.PostAsync(apiEndpoint, content, ct), CancellationToken.None);
        }
    }
}
