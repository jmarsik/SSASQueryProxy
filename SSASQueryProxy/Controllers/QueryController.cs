using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using Microsoft.AnalysisServices.AdomdClient;
using SSASQueryProxy.Filters;

namespace SSASQueryProxy.Controllers
{
    public class QueryController : ApiController
    {
        // semicolon delimited list of allowed servers from web.config
        private static HashSet<string> _allowedServers = ConfigurationManager.AppSettings["allowedSsasServers"].Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpper()).ToHashSet();

        [HttpGet]
        public HttpResponseMessage Get()
        {
            // simple help message when user tries to use GET method, possible even without authentication
            return Request.CreateResponse(HttpStatusCode.OK, "Possible parameters: server, db, localeIdentifier (default 1029, which is English), applicationName (default SSASQueryProxy), timeout (default 120 seconds)");
        }

        [SaveCredsBasicAuthentication]
        [Authorize]
        [HttpPost]
        public HttpResponseMessage Post([FromUri] string server, [FromUri] string db, [FromUri] int localeIdentifier = 1029, [FromUri] string applicationName = "SSASQueryProxy", [FromUri] int timeout = 120)
        {
            // https://weblog.west-wind.com/posts/2013/dec/13/accepting-raw-request-body-content-with-aspnet-web-api
            string query = Request.Content.ReadAsStringAsync().Result;

            Trace.TraceInformation($"{Request.GetCorrelationId()} Got query request from IP {HttpContext.Current?.Request.UserHostAddress} for server {server}, database {db}, locale identifier {localeIdentifier}, application name {applicationName}, timeout {timeout} seconds, query {query?.Substring(0, 512)}");

            // check identity presence (should be handled by SaveCredsBasicAuthentication filter)
            if (!(Request.GetRequestContext().Principal?.Identity is SaveCredsBasicAuthenticationIdentity identity))
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Basic authentication credentials not found in request");
            }

            // check if server is allowed in configuration
            if (!_allowedServers.Contains(server.ToUpper()))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Parameter server contains forbidden value (not in allowed servers list)");
            }

            // check parameters for special characters; not exhausting list of possible characters, but should be sufficient
            if (!Regex.IsMatch(db, @"^[a-zA-Z0-9_-]+$"))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Parameter db contains forbidden characters");
            }

            if (!Regex.IsMatch(applicationName, @"^[a-zA-Z0-9_\-!@#$%^&*()+.:]+$"))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Parameter applicationName contains forbidden characters");
            }

            if (!Regex.IsMatch(identity.Name, @"^[a-zA-Z0-9_\-!#$%^&*()+.]+$"))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "User's username contains forbidden characters");
            }

            if (!Regex.IsMatch(identity.Password, @"^[a-zA-Z0-9_\-!@#$%^&*()+.:]+$"))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "User's password contains forbidden characters");
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // require Pkt Privacy (encrypted connection)
            // set reasonable default for various SSAS parameters
            using (AdomdConnection conn = new AdomdConnection($"Data Source='{server}'; Catalog='{db}'; User ID='{identity.Name}'; Password='{identity.Password}'; Protection Level=Pkt Privacy; " +
                $"Locale Identifier={localeIdentifier}; Application Name='{applicationName}'; " +
                $"Connect Timeout={timeout}; Timeout={timeout}; " +
                $"MDX Compatibility=1; MDX Missing Member Mode=Ignore; VisualMode=0;"))
            {
                var ds = new DataSet();
                ds.EnforceConstraints = false;
                ds.Tables.Add();

                conn.Open();

                string commandText = query;

                using (AdomdCommand cmd = new AdomdCommand(commandText, conn))
                {
                    cmd.CommandTimeout = timeout;

                    using (AdomdDataReader dr = cmd.ExecuteReader())
                    {
                        ds.Tables[0].Load(dr);
                        dr.Close();
                    }
                }

                conn.Close();

                stopwatch.Stop();
                Trace.TraceInformation($"{Request.GetCorrelationId()} Connection to SSAS and processing query took {stopwatch.Elapsed.TotalMilliseconds} ms");

                // will return data in simple structured format, default is JSON
                return Request.CreateResponse(HttpStatusCode.OK, ds.Tables[0]);
            }
        }
    }
}