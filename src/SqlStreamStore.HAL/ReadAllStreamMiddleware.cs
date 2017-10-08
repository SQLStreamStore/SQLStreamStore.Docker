namespace SqlStreamStore.HAL
{
    using Microsoft.Owin;
    using Microsoft.Owin.Builder;
    using Owin;
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

            var builder = new AppBuilder()
                .MapWhen(IsStream, inner => inner.Use(GetStream(allStream)))
                .MapWhen(IsStreamMessage, inner => inner.Use(GetStreamMessage(allStream)));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }

        private static bool IsStream(IOwinContext context)
            => context.IsGetOrHead() && !context.Request.Path.HasValue;

        private static bool IsStreamMessage(IOwinContext context)
            => context.IsGetOrHead() &&  long.TryParse(context.Request.Path.Value?.Remove(0, 1), out var _);

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

        private static MidFunc GetStreamMessage(AllStreamResource allStream) => next => async env =>
        {
            var context = new OwinContext(env);

            var response = await allStream.GetMessage(
                new ReadAllStreamMessageOptions(context.Request),
                context.Request.CallCancelled);

            using(new OptionalHeadRequestWrapper(context))
            {
                await context.WriteHalResponse(response);
            }
        };
    }
}