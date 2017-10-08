namespace SqlStreamStore.HAL
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    internal static class OwinContextExtensions
    {
        private static readonly JsonSerializer s_serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.None
        });

        public static async Task WriteHalResponse(this IOwinContext context, Response response)
        {
            context.Response.ContentType = Constants.Headers.ContentTypes.HalJson;

            context.Response.StatusCode = response.StatusCode;

            foreach(var header in response.Headers)
            {
                context.Response.Headers.AppendValues(header.Key, header.Value);
            }

            using(var writer = new JsonTextWriter(new StreamWriter(context.Response.Body))
            {
                CloseOutput = false
            })
            {
                await response.Hal.ToJObject(s_serializer).WriteToAsync(writer, context.Request.CallCancelled);

                await writer.FlushAsync(context.Request.CallCancelled);
            }
        }

        public static bool IsGetOrHead(this IOwinContext context)
            => context.Request.Method == "GET" || context.Request.Method == "HEAD";

        public static bool IsPost(this IOwinContext context)
            => context.Request.Method == "POST";
        
        public static bool IsDelete(this IOwinContext context)
            => context.Request.Method == "DELETE";

    }
}