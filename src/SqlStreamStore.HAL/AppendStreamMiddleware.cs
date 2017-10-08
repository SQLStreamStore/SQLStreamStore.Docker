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

    internal static class AppendStreamMiddleware
    {
        public static MidFunc UseStreamStore(IStreamStore streamStore)
        {
            var stream = new StreamResource(streamStore);
            
            var builder = new AppBuilder()
                .MapWhen(IsStream, inner => inner.Use(AppendStream(stream)));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }
        
        private static bool IsStream(IOwinContext context)
            => context.IsPost() && context.Request.Path.Value?.Length > 1;

        private static MidFunc AppendStream(StreamResource stream) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = await AppendStreamOptions.Create(context.Request, context.Request.CallCancelled);

            var response = await stream.AppendMessages(options, context.Request.CallCancelled);

            await context.WriteHalResponse(response);
        };
    }
}