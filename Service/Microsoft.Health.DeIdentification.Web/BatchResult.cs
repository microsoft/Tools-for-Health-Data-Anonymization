using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Web
{
    public class BatchResult : ActionResult
    {
        public BatchResult(HttpStatusCode statusCode)
        {
            StatusCode= statusCode;
        }

        public BatchResult(HttpStatusCode statusCode, IDictionary<string, string> headers)
        {
            StatusCode = statusCode;
            foreach (var item in headers)
            {
                Headers.Add(item.Key, item.Value);
            }
        }

        public HttpStatusCode? StatusCode { get; set; }

        public IHeaderDictionary Headers { get; } = new HeaderDictionary();

        public static BatchResult Accept()
        {
            return new BatchResult(HttpStatusCode.Accepted);
        }

        public static BatchResult Accept(IDictionary<string, string> headers)
        {
            return new BatchResult(HttpStatusCode.Accepted, headers);
        }

        public static BatchResult BadRequest()
        {
            return new BatchResult(HttpStatusCode.BadRequest);
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));


            HttpResponse response = context.HttpContext.Response;

            response.StatusCode = (int)StatusCode;

            foreach (var item in Headers)
            {
                response.Headers.Add(item.Key, item.Value);
            }

            ActionResult result = new EmptyResult();

            return result.ExecuteResultAsync(context);
        }
    }
}
