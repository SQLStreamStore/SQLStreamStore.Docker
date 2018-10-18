namespace SqlStreamStore.HAL.Streams
{
    using System.Net.Http;
    using Microsoft.AspNetCore.Builder;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;
    
    internal static class StreamsMiddleware
    {
        public static IApplicationBuilder UseStreams(this IApplicationBuilder builder, IStreamStore streamStore)
        {
            var streams = new StreamResource(streamStore);

            return builder
                .MapWhen(HttpMethod.Get, inner => inner.Use(GetStream(streams)))
                .MapWhen(HttpMethod.Delete, inner => inner.Use(DeleteStream(streams)))
                .MapWhen(HttpMethod.Post, inner => inner.Use(AppendStream(streams)))
                .UseAllowedMethods(streams);
        }

        private static MidFunc GetStream(StreamResource streams) => async (context, next) =>
        {
            var operation = new ReadStreamOperation(context.Request);

            var response = await streams.Get(operation, context.RequestAborted);

            await context.WriteResponse(response);
        };

        private static MidFunc DeleteStream(StreamResource stream) =>
            async (context, next) =>
            {
                var operation = new DeleteStreamOperation(context.Request);

                var response = await stream.Delete(operation, context.RequestAborted);

                await context.WriteResponse(response);
            };

        private static MidFunc AppendStream(StreamResource stream) =>
            async (context, next) =>
            {
                var operation = await AppendStreamOperation.Create(context.Request, context.RequestAborted);

                var response = await stream.Post(operation, context.RequestAborted);

                await context.WriteResponse(response);
            };
    }
}