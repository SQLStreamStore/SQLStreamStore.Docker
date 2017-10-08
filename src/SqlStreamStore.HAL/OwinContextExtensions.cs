namespace SqlStreamStore.HAL
{
    using System.IO;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using Microsoft.IO;
    using Microsoft.Owin;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using SqlStreamStore.Streams;

    internal static class OwinContextExtensions
    {
        private static readonly RecyclableMemoryStreamManager s_StreamManager
            = new RecyclableMemoryStreamManager();

        public static async Task WriteHalResponse(this IOwinContext context, Response response)
        {
            context.Response.ContentType = Constants.Headers.ContentTypes.HalJson;

            context.Response.StatusCode = response.StatusCode;

            foreach(var header in response.Headers)
            {
                context.Response.Headers.AppendValues(header.Key, header.Value);
            }

            using(var stream = s_StreamManager.GetStream())
            using(var writer = new StreamWriter(stream))
            {
                using(var jwriter = new JsonTextWriter(writer) { CloseOutput = false })
                {
                    var serializer = new JsonSerializer
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };

                    serializer.Serialize(jwriter, response.Hal);

                    jwriter.Flush();
                }

                stream.Position = 0;

                await stream.CopyToAsync(context.Response.Body, 8192, context.Request.CallCancelled);
            }
        }

        public static Task WriteWrongExpectedVersion(this IOwinContext context, WrongExpectedVersionException ex)
            => context.WriteHalResponse(new Response(new HALResponse(new
            {
                type = "WrongExpectedVersion",
                title = "Wrong expected version.",
                detail = ex.Message
            }), 409));

        public static bool IsGetOrHead(this IOwinContext context)
            => context.Request.Method == "GET" || context.Request.Method == "HEAD";

        public static bool IsPost(this IOwinContext context)
            => context.Request.Method == "POST";
        
        public static bool IsDelete(this IOwinContext context)
            => context.Request.Method == "DELETE";

    }
}