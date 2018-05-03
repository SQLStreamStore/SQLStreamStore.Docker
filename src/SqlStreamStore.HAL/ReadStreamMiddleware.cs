namespace SqlStreamStore.HAL
{
    using Microsoft.Owin;
    using Microsoft.Owin.Builder;
    using Owin;
    using SqlStreamStore.HAL.Resources;
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
            var streamMessages = new StreamMessageResource(streamStore);

            var builder = new AppBuilder()
                .MapWhen(IsStreamMessage, inner => inner.Use(GetStreamMessage(streamMessages)))
                .MapWhen(IsStream, inner => inner.Use(GetStream(streams)));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }

        private static bool IsStream(IOwinContext context)
            => context.IsGetOrHead() && context.Request.Path.IsStream();

        private static bool IsStreamMessage(IOwinContext context)
            => context.IsGetOrHead() && context.Request.Path.IsStreamMessage();

        private static MidFunc GetStream(StreamResource streams) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = new ReadStreamOptions(context.Request);

            var response = await streams.GetPage(options, context.Request.CallCancelled);

            using(new OptionalHeadRequestWrapper(context))
            {
                await context.WriteHalResponse(response);
            }
        };

        private static MidFunc GetStreamMessage(StreamMessageResource streamMessages) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = new ReadStreamMessageByStreamVersionOptions(context.Request);

            var response = await streamMessages.GetMessage(options, context.Request.CallCancelled);

            using(new OptionalHeadRequestWrapper(context))
            {
                await context.WriteHalResponse(response);
            }
        };
    }
}