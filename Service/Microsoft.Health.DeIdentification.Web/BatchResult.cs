using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    }
}
