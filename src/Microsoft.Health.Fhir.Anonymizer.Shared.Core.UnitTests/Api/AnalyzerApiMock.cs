using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Presidio.Api;
using Presidio.Client;
using Presidio.Model;

namespace Fhir.Anonymizer.Core.UnitTests.Api
{
    
    public class AnalyzerApiMock : IAnalyzerApi
    {
        protected internal const int DefaultStart = 1;
        protected internal const int DefaultEnd = 2;
        protected internal const double DefaultScore = 3.0;
        protected internal const string EntityType = "ENTITY_TYPE";

        public List<RecognizerResultWithAnalysis> AnalyzePost(AnalyzeRequest analyzeRequest)
        {
            return new List<RecognizerResultWithAnalysis>
            {
                new RecognizerResultWithAnalysis(start:DefaultStart, end:DefaultEnd, score:DefaultScore, entityType:EntityType)
            };
        }

        public ApiResponse<List<RecognizerResultWithAnalysis>> AnalyzePostWithHttpInfo(AnalyzeRequest analyzeRequest)
        {
            throw new NotImplementedException();
        }

        public string HealthGet()
        {
            throw new NotImplementedException();
        }

        public ApiResponse<string> HealthGetWithHttpInfo()
        {
            throw new NotImplementedException();
        }

        public List<string> RecognizersGet(string language = null)
        {
            throw new NotImplementedException();
        }

        public ApiResponse<List<string>> RecognizersGetWithHttpInfo(string language = null)
        {
            throw new NotImplementedException();
        }

        public List<string> SupportedentitiesGet(string language = null)
        {
            throw new NotImplementedException();
        }

        public ApiResponse<List<string>> SupportedentitiesGetWithHttpInfo(string language = null)
        {
            throw new NotImplementedException();
        }

        public string GetBasePath()
        {
            throw new NotImplementedException();
        }

        public IReadableConfiguration Configuration { get; set; }
        public ExceptionFactory ExceptionFactory { get; set; }

        public Task<List<RecognizerResultWithAnalysis>> AnalyzePostAsync(AnalyzeRequest analyzeRequest,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<List<RecognizerResultWithAnalysis>>> AnalyzePostWithHttpInfoAsync(
            AnalyzeRequest analyzeRequest,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<string> HealthGetAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<string>> HealthGetWithHttpInfoAsync(
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> RecognizersGetAsync(string language = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<List<string>>> RecognizersGetWithHttpInfoAsync(string language = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> SupportedentitiesGetAsync(string language = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<List<string>>> SupportedentitiesGetWithHttpInfoAsync(string language = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }
    }
}