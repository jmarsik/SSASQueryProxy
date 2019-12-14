# SSAS query proxy

Simple ASP.NET Web API application for full .NET Framework that runs as a **proxy between client** (something that needs data from SQL Server Analysis Services (SSAS) instance) **and the SSAS instance** (the server).
When the client and the server are in the same Active Directory domain, everything works just fine. But when this is not the case, problems start to appear.
There are also different ways of connecting to SSAS - direct connection (using Windows authentication) and HTTP(S) connection with MSMDPUMP ISAPI module (using Basic authentication, maybe Windows authentication is also possible).

The main motivation was a situation with Power BI Service (as of 2019-12). If you are using SSAS independent of your AD domain (ie. hosted in Azure VM for your customers, accessed mainly through HTTPS and MSMDPUMP) and you want to publish Power BI reports to Power BI Service with automated scheduled refresh of the report data, you are screwed. Every single way of connection fails (direct from Power BI Service or through the gateway, direct to SSAS or through MSMDPUMP, using this or that authentication, import mode or DirectQuery mode, using R Script as data source, using Python script as data source, simply everything you can think of).

**That's where SSASQueryProxy comes in.**

You simply deploy it somewhere (Azure Web App, ordinary server, doesn't matter), publish it to the internet via HTTPS and use it as "web content source" in your Power BI report. You set your target server, target SSAS database, username, password and MDX query by URL parameters and/or request body. SSASQueryProxy executes ordinary ADOMD.NET command with your MDX query and returns back results as JSON data.

## Deployment to Azure Web App

Don't forget this:

* set Configuration - General Settings - Platform to 64bit (ADOMD.NET SSAS client NuGet package is 64bit only)
* add item to Configuration - Application Settings named "allowedSsasServers" containing semicolon delimited list of allowed SSAS servers (could be also URLs to MSMDPUMP endpoints)
* add item to Configuration - Application Settings named "SCM\_DO\_BUILD\_DURING\_DEPLOYMENT" containing "true" if you plan to perform deployment by ZIP deploy

You can use Git for deployment or you can just ZIP the repository content and upload it to ZIP deploy page in Azure Web App KUDU console. It's probably the easiest way.

If you need to troubleshoot something, just enable App Service logs - Application Logging (filesystem) and set Level to Verbose. Then go to Log stream and watch your proxy work (or not).

## Usage

### URL, parameters, authentication, output

Proxy uses following URL (when deployed to Azure Web App): https://yourwebappname.azurewebsites.net/?server=YOURSERVER&db=YOURDB&localeIdentifier=1029&applicationName=MYAPP&timeout=900

Method: POST or GET (see below)

Parameters:

* server - server hostname or URL to MSMDPUMP endpoint
* db - SSAS database named
* localeIdentifier - (optional, default 1029, which is English) locale identifier
* applicationName - (optional, default SSASQueryProxy) application identifier to distinguish your application in SSAS traces
* timeout (optional, default 120 seconds) connect timeout and also command timeout in seconds

Credentials:

* pass them with Basic authentication
* or you can use a little trick - manually construct Authorization header with value "Basic XXX" where XXX is BASE64-encoded string "username:password"

MDX query:

* send MDX query in the request body (in that case use POST method)
* or send MDX query in `query` parameter (in that case use GET method), of course you will be limited by maximum URL length in your client software or along the way to the proxy

Output:

* JSON structure with data returned by SSAS
* could be also XML, but you have to prepare your MDX query in a way that doesn't use XML-illegal characters in field names etc (and also specify Accept: application/xml request header)

### Usage in Power BI report / Power Query

Use Web.Contents M language function, see https://docs.microsoft.com/en-us/powerquery-m/web-contents. Unfortunately there is another problem with this function (they are really trying to make this as hard as possible!). You CANNOT use the Content option to specify request body and convert the request to use the POST method IF you are using authentication. What?? You heard it right ... That's why SSASQueryProxy supports 2 methods of passing the MDX query - in the request body and also in query string parameter. And that's why I mention this little trick about Authorization header above ... because you can use it to trick Power BI into thinking that the request is not authenticated (and therefore use the Content option). Crazy right?

The little trick has one disadvantage - you cannot use ordinary Power BI support for credentials, you have to store them in Power Query parameters or somewhere else.

Both ways of usage allow configuring of scheduled refresh when uploaded to Power BI Service. You then specify the credentials or Power Query parameters in dataset settings in Power BI Service.

The M language code follows. First the one with the mentioned trick:

```
Table.FromRecords(Json.Document(Web.Contents(
  "https://yourwebappname.azurewebsites.net/",
  [
    Headers = [#"Authorization" = "Basic " & Binary.ToText(Text.ToBinary(
      ParameterUsername & ":" & ParameterPassword
    ), BinaryEncoding.Base64)],
    Query = [
      server = "YOURSERVERorMSMDPUMPURL",
      db = "YOURDB",
      applicationName = "SSASQueryProxyPBI",
      localeIdentifier = "1029",
      timeout = "900"
    ],
    Content = Text.ToBinary("MDXQUERY")
  ]
)))
```

And the other with ordinary Power BI Basic authentication working:

```
Table.FromRecords(Json.Document(Web.Contents(
  "https://yourwebappname.azurewebsites.net/",
  [
    Query = [
      server = "YOURSERVERorMSMDPUMPURL",
      db = "YOURDB",
      applicationName = "SSASQueryProxyPBI",
      localeIdentifier = "1029",
      timeout = "900",
      query = "MDXQUERY"
    ]
  ]
)))
```

## Testing from command line

While developing the proxy, you can test it from the command line using `curl`:

```
curl.exe --insecure --user "USER:PASSWORD" --data-ascii "MDXQUERY" --verbose "https://localhost:44380/?server=SERVERorHTTPMSMDPUMPURL&db=DATABASE&localeIdentifier=1029&applicationName=SSASQueryProxyDEV&timeout=900"
```
