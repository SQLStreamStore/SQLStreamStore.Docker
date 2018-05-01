namespace SqlStreamStore.HAL
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using Microsoft.Owin.Builder;
    using Owin;
    using MidFunc = System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task
        >, System.Func<System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task>
    >;

    internal static class ReadStreamMiddleware
    {
        public static MidFunc UseStreamStore(IStreamStore streamStore)
        {
            var streams = new StreamResource(streamStore);
            
            var builder = new AppBuilder()
                .MapWhen(IsStreamMessage, inner => inner.Use(GetStreamMessage(streams)))
                .MapWhen(IsStreamMessageOptions, inner => inner.Use(GetStreamMessageOptions))
                .MapWhen(IsStream, inner => inner.Use(GetStream(streams)))
                .MapWhen(IsStreamOptions, inner => inner.Use(GetStreamOptions));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }

        private static bool IsStream(IOwinContext context)
            => context.IsGetOrHead() && context.Request.Path.IsStream();

        private static bool IsStreamOptions(IOwinContext context)
            => context.IsOptions() && context.Request.Path.IsStream();

        private static bool IsStreamMessage(IOwinContext context)
            => context.IsGetOrHead() && context.Request.Path.IsStreamMessage();

        private static bool IsStreamMessageOptions(IOwinContext context)
            => context.IsOptions() && context.Request.Path.IsStreamMessage();

        private static MidFunc GetStream(StreamResource stream) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = new ReadStreamOptions(context.Request);

            var response = await stream.GetPage(options, context.Request.CallCancelled);

            using(new OptionalHeadRequestWrapper(context))
            {
                await context.WriteHalResponse(response);
            }
        };

        private static MidFunc GetStreamOptions => next => env =>
        {
            var context = new OwinContext(env);

            context.SetStandardCorsHeaders(
                HttpMethod.Get,
                HttpMethod.Head,
                HttpMethod.Options,
                HttpMethod.Post,
                HttpMethod.Delete);

            return Task.CompletedTask;
        };

        private static MidFunc GetStreamMessage(StreamResource stream) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = new ReadStreamMessageOptions(context.Request);

            var response = await stream.GetMessage(options, context.Request.CallCancelled);

            using(new OptionalHeadRequestWrapper(context))
            {
                await context.WriteHalResponse(response);
            }
        };

        private static MidFunc GetStreamMessageOptions => next => env =>
        {
            var context = new OwinContext(env);

            context.SetStandardCorsHeaders(
                HttpMethod.Get,
                HttpMethod.Head,
                HttpMethod.Options);

            return Task.CompletedTask;
        };
    }
}