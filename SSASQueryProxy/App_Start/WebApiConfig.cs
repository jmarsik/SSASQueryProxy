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

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}"
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
