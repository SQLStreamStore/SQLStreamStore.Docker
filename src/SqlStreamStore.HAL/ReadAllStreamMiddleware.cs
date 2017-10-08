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
                .MapWhen(IsStream, inner => inner.Use(GetStream(allStream)))
                .MapWhen(IsStreamOptions, inner => inner.Use(GetStreamOptions))
                .MapWhen(IsStreamMessage, inner => inner.Use(GetStreamMessage(allStream)))
                .MapWhen(IsStreamMessageOptions, inner => inner.Use(GetStreamMessageOptions));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }

        private static bool IsStream(PathString requestPath) 
            => !requestPath.HasValue;

        private static bool IsStream(IOwinContext context)
            => context.IsGetOrHead() && IsStream(context.Request.Path);

        private static bool IsStreamOptions(IOwinContext context)
            => context.IsOptions() && IsStream(context.Request.Path);

        private static bool IsStreamMessage(PathString requestPath) 
            => long.TryParse(requestPath.Value?.Remove(0, 1), out var _);

        private static bool IsStreamMessage(IOwinContext context)
            => context.IsGetOrHead() && IsStreamMessage(context.Request.Path);

        private static bool IsStreamMessageOptions(IOwinContext context)
            => context.IsOptions() && IsStreamMessage(context.Request.Path);

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