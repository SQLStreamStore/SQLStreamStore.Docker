namespace SqlStreamStore.HAL
{
    using System;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using Microsoft.Owin;
    using Microsoft.Owin.Builder;
    using Owin;
    using MidFunc = System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task
        >, System.Func<System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task>
    >;

    public static class SqlStreamStoreHalMiddleware
    {
        private static MidFunc AddReasonPhrase => next => env =>
        {
            var context = new OwinContext(env);

            context.Response.OnSendingHeaders(_ =>
                {
                    if(!Constants.ReasonPhrases.TryGetValue(context.Response.StatusCode, out var reasonPhrase))
                    {
                        return;
                    }

                    context.Response.ReasonPhrase = reasonPhrase;
                },
                null);

            return next(env);
        };

        private static MidFunc MethodsNotAllowed(params string[] methods) => next => env =>
        {
            var context = new OwinContext(env);

            if(!methods.Contains(context.Request.Method))
            {
                return next(env);
            }

            context.Response.StatusCode = 405;

            return Task.CompletedTask;
        };

        private static MidFunc AcceptOnlyHalJson => next => env =>
        {
            var context = new OwinContext(env);

            var accept = context.Request.Accept?.Split(',')
                             .Select(value => MediaTypeWithQualityHeaderValue.TryParse(value, out var header)
                                 ? header.MediaType
                                 : null)
                         ?? Enumerable.Empty<string>();

            return accept.Any(header => header == Constants.Headers.ContentTypes.HalJson
                                        || header == Constants.Headers.ContentTypes.Any)
                ? next(env)
                : context.WriteHalResponse(new Response(new HALResponse(new
                    {
                        type = "Not Acceptable",
                        title = "Not Acceptable",
                        detail = $"The server only understands {Constants.Headers.ContentTypes.HalJson}."
                    }),
                    406));
        };

        private static MidFunc Index => next => env =>
        {
            var context = new OwinContext(env);

            if((context.Request.Path.Value ?? "/") != "/")
            {
                return next(env);
            }

            var response = new Response(new HALResponse(null)
                .AddLinks(new Link(Constants.Relations.Feed, "stream"))
                .AddLinks(new Link(Constants.Relations.Self, string.Empty))
                .AddLinks(new Link(Constants.Relations.Index, string.Empty)));

            return context.WriteHalResponse(response);
        };

        public static MidFunc UseSqlStreamStoreHal(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));

            var builder = new AppBuilder()
                .Use(ExceptionHandlingMiddleware.HandleExceptions)
                .Use(AddReasonPhrase)
                .Use(AcceptOnlyHalJson)
                .Use(Index)
                .Map("/stream", UseAllStream(streamStore))
                .Map("/streams", UseStream(streamStore));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }

        private static Action<IAppBuilder> UseStream(IStreamStore streamStore)
            => builder => builder
                .MapWhen(IsOptions, inner => inner.Use(StreamOptionsMiddleware.UseStreamStore(streamStore)))
                .Use(StreamMetadataMiddleware.UseStreamStore(streamStore))
                .Use(ReadStreamMiddleware.UseStreamStore(streamStore))
                .Use(AppendStreamMiddleware.UseStreamStore(streamStore))
                .Use(DeleteStreamMiddleware.UseStreamStore(streamStore))
                .Use(MethodsNotAllowed("TRACE", "PATCH"));

        private static Action<IAppBuilder> UseAllStream(IStreamStore streamStore)
            => builder => builder
                .MapWhen(IsOptions, inner => inner.Use(AllStreamOptionsMiddleware.UseStreamStore(streamStore)))
                .Use(ReadAllStreamMiddleware.UseStreamStore(streamStore))
                .Use(MethodsNotAllowed("POST", "PUT", "DELETE", "TRACE", "PATCH"));

        private static bool IsOptions(IOwinContext context) => context.IsOptions();
    }
}