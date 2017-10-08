namespace SqlStreamStore.HAL
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.IO;
    using Microsoft.Owin;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using SqlStreamStore.Streams;
    
    internal class OptionalHeadRequestWrapper : IDisposable
    {
        private readonly IOwinContext _context;
        private readonly Stream _originalBody;

        public OptionalHeadRequestWrapper(IOwinContext context)
        {
            _context = context;
            _originalBody = _context.Response.Body;
            if(context.Request.Method == "HEAD")
            {
                context.Response.Body = new HeadRequestStream();
            }
        }
        
        public void Dispose()
        {
            _context.Response.Body = _originalBody;
        }

        class HeadRequestStream : Stream
        {
            private long _length;

            public override void Flush() => FlushAsync(CancellationToken.None).Wait();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                _length += count;
                Position += count;
                return Task.CompletedTask;
            }

            public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public override bool CanRead { get; } = false;
            public override bool CanSeek { get; } = false;
            public override bool CanWrite { get; } = true;
            public override long Length => _length;
            public override long Position { get; set; }
        } 
    }

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

        public static Task WriteProblemDetailsResponse(this IOwinContext context, WrongExpectedVersionException ex)
        {
            context.Response.StatusCode = 409;
            context.Response.ReasonPhrase = "Conflict";
            context.Response.ContentType = Constants.Headers.ContentTypes.ProblemDetails;

            return context.Response.WriteAsync(ex.ConvertToProblemDetails(), context.Request.CallCancelled);
        }

        public static bool IsGetOrHead(this IOwinContext context)
            => context.Request.Method == "GET" || context.Request.Method == "HEAD";

        public static bool IsPost(this IOwinContext context)
            => context.Request.Method == "POST";
        
        public static bool IsDelete(this IOwinContext context)
            => context.Request.Method == "DELETE";

    }
}