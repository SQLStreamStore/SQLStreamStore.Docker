namespace SqlStreamStore.HAL
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    public static class SqlStreamStoreHalMiddleware
    {
        private static MidFunc CaseSensitiveQueryStrings => (context, next) =>
        {
            if(context.Request.QueryString != QueryString.Empty)
            {
                var queryString = context.Request.QueryString;
                context.Request.Query = new CaseSensitiveQueryCollection(queryString);
                // Setting context.Request.Query mutates context.Request.QueryString.
                // This has the unfortunate side effect of turning ?a=1&b into ?a=1&b=.
                // so, replace with original context.Request.QueryString and call it a day.
                context.Request.QueryString = queryString;
            }

            return next();
        };

        private static MidFunc MethodsNotAllowed(params string[] methods) => (context, next) =>
        {
            if(!methods.Contains(context.Request.Method))
            {
                return next();
            }

            context.Response.StatusCode = 405;

            return Task.CompletedTask;
        };

        private static MidFunc AcceptHalJson => (context, next) =>
        {
            var accept = context.Request.GetAcceptHeaders();

            return accept.Any(header => header == Constants.Headers.ContentTypes.HalJson
                                        || header == Constants.Headers.ContentTypes.Any)
                ? next()
                : context.WriteHalResponse(new Response(new HALResponse(new
                    {
                        type = "Not Acceptable",
                        title = "Not Acceptable",
                        detail = $"The server only understands {Constants.Headers.ContentTypes.HalJson}."
                    }),
                    406));
        };

        private static MidFunc HeadRequests => async (context, next) =>
        {
            using(new OptionalHeadRequestWrapper(context))
            {
                await next();
            }
        };
        public static IApplicationBuilder UseSqlStreamStoreHal(
            this IApplicationBuilder builder,
            IStreamStore streamStore)
        {
            if(builder == null)
                throw new ArgumentNullException(nameof(builder));
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));

            return builder
                .UseExceptionHandling()
                .Use(CaseSensitiveQueryStrings)
                .Use(AcceptHalJson)
                .Use(HeadRequests)
                .UseIndex()
                .Map("/stream", UseAllStream(streamStore))
                .Map("/streams", UseStream(streamStore));
        }

        private static Action<IApplicationBuilder> UseStream(IStreamStore streamStore)
            => builder => builder
                .MapWhen(IsOptions, inner => inner.UseStreamOptions(streamStore))
                .UseStreamMetadata(streamStore)
                .UseReadStream(streamStore)
                .UseAppendStream(streamStore)
                .UseDeleteStream(streamStore)
                .Use(MethodsNotAllowed("TRACE", "PATCH"));

        private static Action<IApplicationBuilder> UseAllStream(IStreamStore streamStore)
            => builder => builder
                .MapWhen(IsOptions, inner => inner.UseAllStreamOptions(streamStore))
                .UseReadAllStream(streamStore)
                .Use(MethodsNotAllowed("POST", "PUT", "DELETE", "TRACE", "PATCH"));

        private static bool IsOptions(HttpContext context) => context.IsOptions();

        private class OptionalHeadRequestWrapper : IDisposable
        {
            private readonly HttpContext _context;
            private readonly Stream _originalBody;

            public OptionalHeadRequestWrapper(HttpContext context)
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

            private class HeadRequestStream : Stream
            {
                private long _length;

                public override void Flush() => FlushAsync(CancellationToken.None).Wait();
                public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
                public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
                public override void SetLength(long value) => throw new NotImplementedException();
                public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

                public override Task WriteAsync(
                    byte[] buffer,
                    int offset,
                    int count,
                    CancellationToken cancellationToken)
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
    }
}