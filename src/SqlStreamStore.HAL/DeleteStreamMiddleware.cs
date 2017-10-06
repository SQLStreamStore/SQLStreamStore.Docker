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

    internal static class DeleteStreamMiddleware
    {
        public static MidFunc UseStreamStore(IStreamStore streamStore)
        {
            var stream = new StreamResource(streamStore);
            
            var builder = new AppBuilder()
                .MapWhen(IsStream, inner => inner.Use(DeleteStream(stream)));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }
        
        private static bool IsStream(IOwinContext context)
            => context.IsDelete() && context.Request.Path.Value?.Length > 1;
        
        
        private static MidFunc DeleteStream(StreamResource stream) => next => async env =>
        {
            var context = new OwinContext(env);

            var options = new DeleteStreamOptions(context.Request);

            try
            {
                var response = await stream.Delete(options, context.Request.CallCancelled);

                await context.WriteHalResponse(response);
            }
            catch(WrongExpectedVersionException ex)
            {
                await context.WriteProblemDetailsResponse(ex);
            }
        };
    }
}