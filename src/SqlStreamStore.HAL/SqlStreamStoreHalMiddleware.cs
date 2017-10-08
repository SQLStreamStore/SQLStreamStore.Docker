namespace SqlStreamStore.HAL
{
    using System;
    using System.Linq;
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
        private static MidFunc AccessControl => next => env =>
        {
            var context = new OwinContext(env);

            context.Response.OnSendingHeaders(_ =>
                {
                    context.Response.Headers["Access-Control-Allow-Methods"] = "GET, HEAD, OPTIONS";
                    context.Response.Headers["Access-Control-Allow-Headers"]
                        = "Content-Type, X-Requested-With, Authorization";
                    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                },
                null);

            return next(env);
        };

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

        private static MidFunc Index => next => env =>
        {
            var context = new OwinContext(env);

            if((context.Request.Path.Value ?? "/") != "/")
            {
                return next(env);
            }

            var response = new Response(new HALResponse(null).AddLinks(new Link("streamStore:stream", "stream")));

            return context.WriteHalResponse(response);
        };

        public static MidFunc UseSqlStreamStoreHal(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));

            var builder = new AppBuilder()
                .Use(ExceptionHandlingMiddleware.HandleExceptions)
                .Use(AccessControl)
                .Use(AddReasonPhrase)
                .Use(Index)
                .Map("/stream", inner => inner
                    .Use(ReadAllStreamMiddleware.UseStreamStore(streamStore))
                    .Use(MethodsNotAllowed("POST", "PUT", "DELETE", "TRACE", "PATCH", "OPTIONS")))
                .Map("/streams", inner => inner
                    .Use(ReadStreamMiddleware.UseStreamStore(streamStore))
                    .Use(AppendStreamMiddleware.UseStreamStore(streamStore))
                    .Use(DeleteStreamMiddleware.UseStreamStore(streamStore))
                    .Use(MethodsNotAllowed("DELETE", "TRACE", "PATCH", "OPTIONS")));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }

        private static MidFunc MethodsNotAllowed(params string[] methods)
            => next => env =>
            {
                var context = new OwinContext(env);

                if(!methods.Contains(context.Request.Method))
                {
                    return next(env);
                }

                context.Response.StatusCode = 405;

                return Task.CompletedTask;
            };
    }
}