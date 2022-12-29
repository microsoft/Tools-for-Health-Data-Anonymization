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

        public HttpStatusCode? StatusCode { get; set; }

        public static BatchResult Accept()
        {
            return new BatchResult(HttpStatusCode.Accepted);
        }

        public static BatchResult BadRequest()
        {
            return new BatchResult(HttpStatusCode.BadRequest);
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));


            HttpResponse response = context.HttpContext.Response;

            response.StatusCode = (int)HttpStatusCode.Accepted;


            ActionResult result = new EmptyResult();

            return result.ExecuteResultAsync(context);
        }
    }
}
