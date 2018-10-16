namespace SqlStreamStore.HAL.AllStreamMessage
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class ReadAllStreamMessageMiddleware
    {
        public static IApplicationBuilder UseReadAllStreamMessage(
            this IApplicationBuilder builder,
            IStreamStore streamStore)
        {
            var allStreamMessages = new AllStreamMessageResource(streamStore);

            return builder
                .MapWhen(IsGetAllStreamMessage, inner => inner.Use(GetStreamMessage(allStreamMessages)))
                .MapWhen(IsOptionsAllStreamMessage, inner => inner.UseOptions(allStreamMessages));
        }

        private static bool IsGetAllStreamMessage(HttpContext context)
            => context.IsGetOrHead() && context.Request.Path.IsAllStreamMessage();

        private static bool IsOptionsAllStreamMessage(HttpContext context)
            => context.IsGetOrHead() && context.Request.Path.IsAllStreamMessage();

        private static MidFunc GetStreamMessage(AllStreamMessageResource allStreamMessages) => async (context, next) =>
        {
            var response = await allStreamMessages.Get(
                new ReadAllStreamMessageOperation(context.Request),
                context.RequestAborted);

            await context.WriteResponse(response);
        };

    }
}