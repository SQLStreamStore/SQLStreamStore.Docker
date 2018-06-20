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

    internal static class DeleteStreamMiddleware
    {
        public static MidFunc UseStreamStore(IStreamStore streamStore)
        {
            var streams = new StreamResource(streamStore);
            var streamMessages = new StreamMessageResource(streamStore);
            
            var builder = new AppBuilder()
                .MapWhen(IsStream, inner => inner.Use(DeleteStream(streams)))
                .MapWhen(IsStreamMessage, inner => inner.Use(DeleteStreamMessage(streamMessages)));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }
        
        private static bool IsStream(IOwinContext context)
            => context.IsDelete() && context.Request.Path.IsStream();

        private static bool IsStreamMessage(IOwinContext context)
            => context.IsDelete() && context.Request.Path.IsStreamMessageByIdOrVersion();

        private static MidFunc DeleteStream(StreamResource stream) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = new DeleteStreamOperation(context.Request);

            var response = await stream.Delete(options, context.Request.CallCancelled);

            await context.WriteHalResponse(response);
        };

        private static MidFunc DeleteStreamMessage(StreamMessageResource streamMessages) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = new DeleteStreamMessageOperation(context.Request);

            var response = await streamMessages.DeleteMessage(options, context.Request.CallCancelled);

            await context.WriteHalResponse(response);
        };
    }
}