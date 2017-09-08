namespace SqlStreamStore.HAL
{
    using System;
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
                    context.Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
                    context.Response.Headers["Access-Control-Allow-Headers"]
                        = "Content-Type, X-Requested-With, Authorization";
                    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
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

        public static MidFunc UseSqlStreamStoreHal(IReadonlyStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));

            var builder = new AppBuilder()
                .Use(AccessControl)
                .Use(Index)
                .Map("/stream", inner => inner.Use(AllStreamMiddleware.UseStreamStore(streamStore)))
                .Map("/streams", inner => inner.Use(StreamMiddleware.UseStreamStore(streamStore)));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }
    }
}