namespace SqlStreamStore.HAL
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using SqlStreamStore.HAL.AllStreamMessage;
    using SqlStreamStore.HAL.Resources;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class ReadAllStreamMiddleware
    {
        public static IApplicationBuilder UseReadAllStream(
            this IApplicationBuilder builder,
            IStreamStore streamStore,
            SqlStreamStoreMiddlewareOptions options)
        {
            var allStream = new AllStreamResource(streamStore, options.UseCanonicalUrls);
            var allStreamMessages = new AllStreamMessageResource(streamStore);

            return builder
                .MapWhen(IsAllStream, inner => inner.Use(GetStream(allStream)))
                .MapWhen(IsAllStreamMessage, inner => inner.Use(GetStreamMessage(allStreamMessages)));
        }

        private static bool IsAllStream(HttpContext context)
            => context.IsGetOrHead() && context.Request.Path.IsAllStream();

        private static bool IsAllStreamMessage(HttpContext context)
            => context.IsGetOrHead() && context.Request.Path.IsAllStreamMessage();


        private static MidFunc GetStream(AllStreamResource allStream) => async (context, next) =>
        {
            var options = new ReadAllStreamOperation(context.Request);

            var response = await allStream.Get(options, context.RequestAborted);

            await context.WriteResponse(response);
        };

        private static MidFunc GetStreamMessage(AllStreamMessageResource allStreamMessages) => async (context, next) =>
        {
            var response = await allStreamMessages.Get(
                new ReadAllStreamMessageOperation(context.Request),
                context.RequestAborted);

            await context.WriteResponse(response);
        };
    }
}