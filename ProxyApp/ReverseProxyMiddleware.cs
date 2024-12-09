using Microsoft.AspNetCore.Http;
using ProxyApp.Models;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProxyApp
{
    public class ReverseProxyMiddleware
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly RequestDelegate _nextMiddleware;

        private readonly IDatabase _redis;

        public ReverseProxyMiddleware(RequestDelegate nextMiddleware, IConnectionMultiplexer muxer)
        {
            _nextMiddleware = nextMiddleware;
            _redis = muxer.GetDatabase();
        }

        public async Task Invoke(HttpContext context, DatabaseContext db)
        {
            //check accessing api
            if (context.Request.Path.HasValue && context.Request.Path.Value.StartsWith("/arcservertest"))
            {
                //get accessed service
                var service = GetService(context.Request.Path.Value);   

                //uri to proxy
                var targetUri = BuildTargetUri(context.Request);

                //count of the processed requests to the service
                var processed_count  = _redis.StringGet(service);

                //access the rule to the service 
                var rule_count = GetRuleCount(service, db);

                //check if the request is allowed
                if (processed_count.IsNull || ((int)processed_count) < rule_count)
                {
                    if (targetUri != null)
                    {
                        //create request to the server
                        var targetRequestMessage = CreateTargetMessage(context, targetUri);

                        //request to the server
                        using (var responseMessage = await _httpClient.SendAsync(targetRequestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
                        {
                            context.Response.StatusCode = (int)responseMessage.StatusCode;
                            if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                var val = _redis.StringIncrement(service);
                            }

                            //response to the proxying request
                            CopyFromTargetResponseHeaders(context, responseMessage);
                            await ProcessResponseContent(context, responseMessage);
                        }
                        return;
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.TooManyRequests;
                }
            }
            await _nextMiddleware(context);
        }

        private HttpRequestMessage CreateTargetMessage(HttpContext context, Uri targetUri)
        {
            var requestMessage = new HttpRequestMessage();
            CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

            requestMessage.RequestUri = targetUri;
            requestMessage.Headers.Host = targetUri.Host;
            requestMessage.Method = GetMethod(context.Request.Method);

            return requestMessage;
        }

        private void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
        {
            var requestMethod = context.Request.Method;

            if (!HttpMethods.IsGet(requestMethod) &&
              !HttpMethods.IsHead(requestMethod) &&
              !HttpMethods.IsDelete(requestMethod) &&
              !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(context.Request.Body);
                requestMessage.Content = streamContent;
            }

            foreach (var header in context.Request.Headers)
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        private void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            context.Response.Headers.Remove("transfer-encoding");

            //for CORS
            context.Response.Headers["Access-Control-Allow-Origin"] = "http://localhost:3000";
        }
        private static HttpMethod GetMethod(string method)
        {
            if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
            if (HttpMethods.IsGet(method)) return HttpMethod.Get;
            if (HttpMethods.IsHead(method)) return HttpMethod.Head;
            if (HttpMethods.IsOptions(method)) return HttpMethod.Options;
            if (HttpMethods.IsPost(method)) return HttpMethod.Post;
            if (HttpMethods.IsPut(method)) return HttpMethod.Put;
            if (HttpMethods.IsTrace(method)) return HttpMethod.Trace;
            return new HttpMethod(method);
        }

        private Uri BuildTargetUri(HttpRequest request)
        {
            Uri targetUri = null;

            targetUri = new Uri("https://portaltest.gismap.by" + request.Path + request.QueryString);

            return targetUri;
        }
        private async Task ProcessResponseContent(HttpContext context, HttpResponseMessage responseMessage)
        {
            var content = await responseMessage.Content.ReadAsByteArrayAsync();

            await context.Response.Body.WriteAsync(content);
        }

        private bool IsContentOfType(HttpResponseMessage responseMessage, string type)
        {
            var result = false;

            if (responseMessage.Content?.Headers?.ContentType != null)
            {
                result = responseMessage.Content.Headers.ContentType.MediaType == type;
            }

            return result;
        }

        private string GetService(string host)
        {
            var arr = host.Split("/");
            return arr[4];
        }

        private int GetRuleCount(string service, DatabaseContext db)
        {
            var rule = db.Rules.Where(rule => rule.Id == 1).First();
            switch (service)
            {
                case "C01_Belarus_WGS84":
                    return rule.allowed_C01_Belarus_WGS84;
                case "A06_ATE_TE_WGS84":
                    return rule.allowed_A06_ATE_TE_WGS84;
                case "A05_EGRNI_WGS84":
                    return rule.allowed_A05_EGRNI_WGS84;
                case "A01_ZIS_WGS84":
                    return rule.allowed_A01_ZIS_WGS84;
                default:
                    return 0;
            }
        }
    }
}
