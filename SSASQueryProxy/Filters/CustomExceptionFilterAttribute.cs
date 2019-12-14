using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using System.Diagnostics;

namespace SSASQueryProxy.Filters
{
    public class CustomExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            // log exception to trace
            Trace.TraceError($"{actionExecutedContext.Request.GetCorrelationId()} Exception occured: {actionExecutedContext.Exception}");

            // output of exception details to the calling client is handled in base class and configured in WebApiConfig.Register
            base.OnException(actionExecutedContext);
        }
    }
}