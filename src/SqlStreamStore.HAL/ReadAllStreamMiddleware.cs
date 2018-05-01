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

    internal static class ReadAllStreamMiddleware
    {
        public static MidFunc UseStreamStore(IReadonlyStreamStore streamStore)
        {
            var allStream = new AllStreamResource(streamStore);
            var allStreamMessages = new AllStreamMessageResource(streamStore);

            var builder = new AppBuilder()
                .MapWhen(IsAllStream, inner => inner.Use(GetStream(allStream)))
                .MapWhen(IsAllStreamMessage, inner => inner.Use(GetStreamMessage(allStreamMessages)));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }

        private static bool IsAllStream(IOwinContext context)
            => context.IsGetOrHead() && context.Request.Path.IsAllStream();

        private static bool IsAllStreamMessage(IOwinContext context)
            => context.IsGetOrHead() && context.Request.Path.IsAllStreamMessage();


        private static MidFunc GetStream(AllStreamResource allStream) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = new ReadAllStreamOptions(context.Request);

            var response = await allStream.GetPage(options, context.Request.CallCancelled);

            using(new OptionalHeadRequestWrapper(context))
            {
                await context.WriteHalResponse(response);
            }
        };
        
        private static MidFunc GetStreamMessage(AllStreamMessageResource allStreamMessages) => next => async env =>
        {
            var context = new OwinContext(env);

            var response = await allStreamMessages.GetMessage(
                new ReadAllStreamMessageOptions(context.Request),
                context.Request.CallCancelled);

            using(new OptionalHeadRequestWrapper(context))
            {
                await context.WriteHalResponse(response);
            }
        };
    }
}