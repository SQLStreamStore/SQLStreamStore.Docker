namespace SqlStreamStore.HAL
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using SqlStreamStore.HAL.Resources;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class ReadAllStreamMiddleware
    {
        public static IApplicationBuilder UseReadAllStream(this IApplicationBuilder builder, IStreamStore streamStore)
        {
            var allStream = new AllStreamResource(streamStore);
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

            var response = await allStream.GetPage(options, context.RequestAborted);

            using(new OptionalHeadRequestWrapper(context))
            {
                await context.WriteHalResponse(response);
            }
        };

        private static MidFunc GetStreamMessage(AllStreamMessageResource allStreamMessages) => async (context, next) => 
        {
            var response = await allStreamMessages.GetMessage(
                new ReadAllStreamMessageOperation(context.Request),
                context.RequestAborted);

            using(new OptionalHeadRequestWrapper(context))
            {
                await context.WriteHalResponse(response);
            }
        };
    }
}