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

    internal static class AllStreamMiddleware
    {
        public static MidFunc UseStreamStore(IReadonlyStreamStore streamStore)
        {
            var allStream = new AllStreamResource(streamStore);

            var builder = new AppBuilder()
                .MapWhen(context => !context.Request.Path.HasValue, inner => inner.Use(GetStream(allStream)))
                .MapWhen(context =>
                    {
                        long _;
                        return long.TryParse(context.Request.Path.Value?.Remove(0, 1), out _);
                    },
                    inner => inner.Use(GetStreamMessage(allStream)));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }

        private static MidFunc GetStream(AllStreamResource allStream) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = new ReadAllStreamOptions(context.Request);

            var response = await allStream.GetPage(options, context.Request.CallCancelled);

            await context.WriteHalResponse(response);
        };

        private static MidFunc GetStreamMessage(AllStreamResource allStream) => next => async env =>
        {
            var context = new OwinContext(env);

            var resource = await allStream.GetMessage(
                new ReadAllStreamMessageOptions(context.Request),
                context.Request.CallCancelled);

            await context.WriteHalResponse(resource);
        };
    }
}