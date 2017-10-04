namespace SqlStreamStore.HAL
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.IO;
    using Microsoft.Owin;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    internal static class OwinContextExtensions
    {
        private static readonly RecyclableMemoryStreamManager s_StreamManager
            = new RecyclableMemoryStreamManager();

        public static async Task WriteHalResponse(this IOwinContext context, Response response)
        {
            context.Response.ContentType = "application/hal+json";

            context.Response.StatusCode = response.StatusCode;

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

        public static bool IsGetOrHead(this IOwinContext context)
            => context.Request.Method == "GET" || context.Request.Method == "HEAD";

        public static bool IsPost(this IOwinContext context)
            => context.Request.Method == "POST";
    }
}