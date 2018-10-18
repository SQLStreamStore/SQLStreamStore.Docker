namespace SqlStreamStore.HAL.AllStream
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class ReadAllStreamMiddleware
    {
        public static IApplicationBuilder UseAllStream(
            this IApplicationBuilder builder,
            IStreamStore streamStore,
            SqlStreamStoreMiddlewareOptions options)
        {
            var allStream = new AllStreamResource(streamStore, options.UseCanonicalUrls);

            return builder
                .MapWhen(IsGet, inner => inner.Use(GetStream(allStream)))
                .UseAllowedMethods(allStream);
        }

        private static bool IsGet(HttpContext context) => context.IsGetOrHead();

        private static MidFunc GetStream(AllStreamResource allStream) => async (context, next) =>
        {
            var options = new ReadAllStreamOperation(context.Request);

            var response = await allStream.Get(options, context.RequestAborted);

            await context.WriteResponse(response);
        };
    }
}