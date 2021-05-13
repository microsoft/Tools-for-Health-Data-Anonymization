using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Presidio.Api;
using Presidio.Client;
using Presidio.Model;

namespace Fhir.Anonymizer.Core.UnitTests.Api
{
        public class AnonymizerApiMock : IAnonymizerApi
        {
            protected internal const string DefaultText = "Anonymized Text";
            protected internal static readonly List<OperatorEntity> s_defaultOperatorEntities = new List<OperatorEntity>();

            public AnonymizeResponse AnonymizePost(AnonymizeRequest anonymizeRequest)
            {
                return new AnonymizeResponse(DefaultText, s_defaultOperatorEntities);
            }

            public ApiResponse<AnonymizeResponse> AnonymizePostWithHttpInfo(AnonymizeRequest anonymizeRequest)
            {
                throw new NotImplementedException();
            }

            public List<string> AnonymizersGet()
            {
                throw new NotImplementedException();
            }

            public ApiResponse<List<string>> AnonymizersGetWithHttpInfo()
            {
                throw new NotImplementedException();
            }

            public List<string> DeanonymizersGet()
            {
                throw new NotImplementedException();
            }

            public ApiResponse<List<string>> DeanonymizersGetWithHttpInfo()
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

            public string GetBasePath()
            {
                throw new NotImplementedException();
            }

            public IReadableConfiguration Configuration { get; set; }
            public ExceptionFactory ExceptionFactory { get; set; }

            public Task<AnonymizeResponse> AnonymizePostAsync(AnonymizeRequest anonymizeRequest,
                CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<ApiResponse<AnonymizeResponse>> AnonymizePostWithHttpInfoAsync(AnonymizeRequest anonymizeRequest,
                CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<List<string>> AnonymizersGetAsync(CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<ApiResponse<List<string>>> AnonymizersGetWithHttpInfoAsync(CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<List<string>> DeanonymizersGetAsync(CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<ApiResponse<List<string>>> DeanonymizersGetWithHttpInfoAsync(CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<string> HealthGetAsync(CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<ApiResponse<string>> HealthGetWithHttpInfoAsync(CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }
        }

        

}