using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.Health.JobManagement;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Web
{
    public static class BatchResultExtension
    {
        public static BatchResult SetContentLocationHeader(this BatchResult result, string operationName, List<JobInfo> jobInfos)
        {
            EnsureArg.IsNotNull(result, nameof(result));
            EnsureArg.IsNotNullOrWhiteSpace(operationName, nameof(operationName));
            StringBuilder urls = new StringBuilder();
            foreach (var item in jobInfos)
            {
                urls.Append(ResolveOperationResultUrl(operationName, item.Id.ToString()).ToString());
                urls.Append(',');
            }

            result.Headers.Add(HeaderNames.ContentLocation, urls.ToString());
            return result;
        }

        public static Uri ResolveOperationResultUrl(string operationName, string id)
        {
            string routeName = string.Empty;
            switch (operationName)
            {
                case OperationConstants.Fhir:
                    routeName = RouteNames.GetDeIdentificationJobStatus;
                    break;
                default:
                    throw new InvalidOperationException($"Not support operationName: {operationName}");
            }
            var url = new Uri($"{RouteNames.BaseUrl}{OperationConstants.Fhir}?{routeName}={id}");
            return url;
        }
    }
}
