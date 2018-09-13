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

    internal static class DeleteStreamMiddleware
    {
        public static IApplicationBuilder UseDeleteStream(this IApplicationBuilder builder, IStreamStore streamStore)
        {
            var streams = new StreamResource(streamStore);
            var streamMessages = new StreamMessageResource(streamStore);
            
            return builder
                .MapWhen(IsStream, inner => inner.Use(DeleteStream(streams)))
                .MapWhen(IsStreamMessage, inner => inner.Use(DeleteStreamMessage(streamMessages)));
        }
        
        private static bool IsStream(HttpContext context)
            => context.IsDelete() && context.Request.Path.IsStream();

        private static bool IsStreamMessage(HttpContext context)
            => context.IsDelete() && context.Request.Path.IsStreamMessageByIdOrVersion();

        private static MidFunc DeleteStream(StreamResource stream) => async (context, next) => 
        {
            var options = new DeleteStreamOperation(context.Request);

            var response = await stream.Delete(options, context.RequestAborted);

            await context.WriteResponse(response);
        };

        private static MidFunc DeleteStreamMessage(StreamMessageResource streamMessages) => async (context, next) =>
        {
            var options = new DeleteStreamMessageOperation(context.Request);

            var response = await streamMessages.DeleteMessage(options, context.RequestAborted);

            await context.WriteResponse(response);
        };
    }
}