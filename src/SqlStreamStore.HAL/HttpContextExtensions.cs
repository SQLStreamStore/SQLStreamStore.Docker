namespace SqlStreamStore.HAL
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using SqlStreamStore.Streams;

    internal static class HttpContextExtensions
    {
        private static readonly JsonSerializer s_serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore
        });

        private static readonly string[] s_NotModifiedRequiredHeaders =
        {
            "cache-control",
            "content-location",
            "date",
            "etag",
            "expires",
            "vary"
        };

        public static Task WriteResponse(this HttpContext context, Response response)
            => context.Request.IfNoneMatch(response)
                ? WriteNotModifiedResponse(context, response)
                : WriteHalResponse(context, response);

        private static bool IfNoneMatch(this HttpRequest request, Response response)
        {
            if(!request.Headers.TryGetValue(Constants.Headers.IfNoneMatch, out var ifNoneMatch)
               || !response.Headers.TryGetValue(Constants.Headers.ETag, out var eTags)
               || eTags.Length == 0)
            {
                return false;
            }

            foreach(var candidate in ifNoneMatch)
            {
                if(string.Equals(eTags[0], candidate, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static Task WriteNotModifiedResponse(HttpContext context, Response response)
        {
            context.Response.StatusCode = 304;
            foreach(var header in s_NotModifiedRequiredHeaders.Where(response.Headers.Keys.Contains))
            {
                context.Response.Headers.Append(header, response.Headers[header]);
            }

            return Task.CompletedTask;
        }

        private static async Task WriteHalResponse(HttpContext context, Response response)
        {
            context.Response.StatusCode = response.StatusCode;

            context.Response.ContentType = Constants.Headers.ContentTypes.HalJson;

            foreach(var header in response.Headers)
            {
                context.Response.Headers.Append(header.Key, header.Value);
            }

            using(var writer = new JsonTextWriter(new StreamWriter(context.Response.Body))
            {
                CloseOutput = false
            })
            {
                await response.Hal.ToJObject(s_serializer).WriteToAsync(writer, context.RequestAborted);

                await writer.FlushAsync(context.RequestAborted);
            }
        }

        public static void SetStandardCorsHeaders(this HttpContext context, params HttpMethod[] allowedMethods)
        {
            if(allowedMethods?.Length > 0)
            {
                context.Response.Headers.Append(
                    "Access-Control-Allow-Methods",
                    Array.ConvertAll(allowedMethods, _ => _.Method));
            }

            context.Response.Headers.Append(
                "Access-Control-Allow-Headers",
                new[]
                {
                    "Content-Type",
                    "X-Requested-With",
                    "Authorization"
                });

            context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        }

        public static bool IsGetOrHead(this HttpContext context)
            => context.Request.Method == "GET" || context.Request.Method == "HEAD";

        public static bool IsPost(this HttpContext context)
            => context.Request.Method == "POST";

        public static bool IsDelete(this HttpContext context)
            => context.Request.Method == "DELETE";

        public static bool IsOptions(this HttpContext context)
            => context.Request.Method == "OPTIONS";

        public static int GetExpectedVersion(this HttpRequest request)
            => int.TryParse(
                request.Headers[Constants.Headers.ExpectedVersion],
                out var expectedVersion)
                ? expectedVersion
                : ExpectedVersion.Any;

        public static string[] GetAcceptHeaders(this HttpRequest contextRequest)
            => Array.ConvertAll(
                contextRequest.Headers
                    .GetCommaSeparatedValues("Accept"),
                value => MediaTypeWithQualityHeaderValue.TryParse(value, out var header)
                    ? header.MediaType
                    : null);
    }
}