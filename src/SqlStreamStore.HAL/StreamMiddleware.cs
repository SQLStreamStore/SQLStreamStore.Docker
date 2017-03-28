namespace SqlStreamStore.HAL
{
    using Microsoft.Owin;
    using Microsoft.Owin.Builder;
    using Owin;
    using SqlStreamStore.Streams;
    using MidFunc = System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task
        >, System.Func<System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task>
    >;

    internal static class StreamMiddleware
    {
        public static MidFunc UseStreamStore(IReadonlyStreamStore streamStore)
        {
            var streams = new StreamResource(streamStore);

            var builder = new AppBuilder()
                .MapWhen(context => context.Request.Path.Value?.Split('/')?.Length == 3,
                    inner => inner.Use(GetStreamMessage(streams)))
                .MapWhen(context => context.Request.Path.Value?.Length > 1, inner => inner.Use(GetStream(streams)));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }

        private static MidFunc GetStream(StreamResource stream) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = new ReadStreamOptions(context.Request);

            var response = await stream.GetPage(options, context.Request.CallCancelled);

            await context.WriteHalResponse(response);
        };

        private static MidFunc GetStreamMessage(StreamResource stream) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = new ReadStreamMessageOptions(context.Request);

            var response = await stream.GetMessage(options, context.Request.CallCancelled);

            if(options.StreamVersion == StreamVersion.End)
            {
                context.Response.StatusCode = 307;
                context.Response.ReasonPhrase = "Moved Temporarily";
                context.Response.Headers["Location"] = $"{((dynamic) response.Hal.Model).StreamVersion}";

                return;
            }

            await context.WriteHalResponse(response);
        };
    }
}