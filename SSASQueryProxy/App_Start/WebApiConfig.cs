using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace SSASQueryProxy
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // output full exception details to the calling client
            // this is a simple proxy application, we have nothing to hide and it will be easier to debug things in this way
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // map root URL to the one and only controller for query proxying
            config.Routes.MapHttpRoute(
                name: "Root",
                routeTemplate: "",
                defaults: new { controller = "Query" }
            );
        }
    }
}
