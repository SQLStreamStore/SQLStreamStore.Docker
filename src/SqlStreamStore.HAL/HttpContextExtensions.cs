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

        public static async Task WriteHalResponse(this HttpContext context, Response response)
        {
            context.Response.ContentType = Constants.Headers.ContentTypes.HalJson;

            context.Response.StatusCode = response.StatusCode;

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