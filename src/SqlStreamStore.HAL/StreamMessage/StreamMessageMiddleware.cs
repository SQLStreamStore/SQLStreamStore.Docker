namespace SqlStreamStore.HAL.StreamMessage
{
    using System.Net.Http;
    using Microsoft.AspNetCore.Builder;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class StreamMessageMiddleware
    {
        public static IApplicationBuilder UseStreamMessages(this IApplicationBuilder builder, IStreamStore streamStore)
        {
            var streamMessages = new StreamMessageResource(streamStore);
            return builder
                .MapWhen(HttpMethod.Get, inner => inner.Use(GetStreamMessage(streamMessages)))
                .MapWhen(HttpMethod.Delete, inner => inner.Use(DeleteStreamMessage(streamMessages)))
                .UseAllowedMethods(streamMessages);
        }

        private static MidFunc GetStreamMessage(StreamMessageResource streamMessages) => async (context, next) =>
        {
            var options = new ReadStreamMessageByStreamVersionOperation(context.Request);

            var response = await streamMessages.Get(options, context.RequestAborted);

            await context.WriteResponse(response);
        };

        private static MidFunc DeleteStreamMessage(StreamMessageResource streamMessages) => async (context, next) =>
        {
            var options = new DeleteStreamMessageOperation(context.Request);

            var response = await streamMessages.Delete(options, context.RequestAborted);

            await context.WriteResponse(response);
        };
    }
}