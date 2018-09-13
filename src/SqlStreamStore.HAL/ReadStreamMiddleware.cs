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

    internal static class ReadStreamMiddleware
    {
        public static IApplicationBuilder UseReadStream(this IApplicationBuilder builder, IStreamStore streamStore)
        {
            var streams = new StreamResource(streamStore);
            var streamMessages = new StreamMessageResource(streamStore);

            return builder
                .MapWhen(IsStreamMessage, inner => inner.Use(GetStreamMessage(streamMessages)))
                .MapWhen(IsStream, inner => inner.Use(GetStream(streams)));
        }

        private static bool IsStream(HttpContext context)
            => context.IsGetOrHead() && context.Request.Path.IsStream();

        private static bool IsStreamMessage(HttpContext context)
            => context.IsGetOrHead() && context.Request.Path.IsStreamMessage();

        private static MidFunc GetStream(StreamResource streams) => async (context, next) =>
        {
            var options = new ReadStreamOperation(context.Request);

            var response = await streams.Get(options, context.RequestAborted);

            await context.WriteResponse(response);
        };

        private static MidFunc GetStreamMessage(StreamMessageResource streamMessages) => async (context, next) =>
        {
            var options = new ReadStreamMessageByStreamVersionOperation(context.Request);

            var response = await streamMessages.Get(options, context.RequestAborted);

            await context.WriteResponse(response);
        };
    }
}