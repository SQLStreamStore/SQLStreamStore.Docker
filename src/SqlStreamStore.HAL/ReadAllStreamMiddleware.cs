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

    internal static class ReadAllStreamMiddleware
    {
        public static MidFunc UseStreamStore(IReadonlyStreamStore streamStore)
        {
            var allStream = new AllStreamResource(streamStore);

            var builder = new AppBuilder()
                .MapWhen(IsAllStream, inner => inner.Use(GetStream(allStream)))
                .MapWhen(IsStreamOptions, inner => inner.Use(GetStreamOptions))
                .MapWhen(IsAllStreamMessage, inner => inner.Use(GetStreamMessage(allStream)))
                .MapWhen(IsStreamMessageOptions, inner => inner.Use(GetStreamMessageOptions));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }

        private static bool IsAllStream(IOwinContext context)
            => context.IsGetOrHead() && context.Request.Path.IsAllStream();

        private static bool IsStreamOptions(IOwinContext context)
            => context.IsOptions() && context.Request.Path.IsAllStream();

        private static bool IsAllStreamMessage(IOwinContext context)
            => context.IsGetOrHead() && context.Request.Path.IsAllStreamMessage();

        private static bool IsStreamMessageOptions(IOwinContext context)
            => context.IsOptions() && context.Request.Path.IsAllStreamMessage();

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
        
        private static MidFunc GetStreamOptions => next => env =>
        {
            var context = new OwinContext(env);
            
            context.SetStandardCorsHeaders(
                HttpMethod.Get, 
                HttpMethod.Head, 
                HttpMethod.Options);

            return Task.CompletedTask;
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