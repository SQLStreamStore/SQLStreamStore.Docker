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

    internal static class StreamMetadataMiddleware
    {
        public static IApplicationBuilder UseStreamMetadata(this IApplicationBuilder builder, IStreamStore streamStore)
        {
            var streamsMetadata = new StreamMetadataResource(streamStore);

            return builder
                .MapWhen(IsSetStreamMetadata, inner => inner.Use(SetStreamMetadata(streamsMetadata)))
                .MapWhen(IsGetStreamMetadata, inner => inner.Use(GetStreamMetadata(streamsMetadata)));
        }

        private static bool IsSetStreamMetadata(HttpContext context)
            => context.IsPost() && context.Request.Path.IsStreamMetadata();

        private static bool IsGetStreamMetadata(HttpContext context)
            => context.IsGetOrHead() && context.Request.Path.IsStreamMetadata();

        private static MidFunc SetStreamMetadata(StreamMetadataResource streamsMetadata)
            => async (context, next) =>
            {
                var options = await SetStreamMetadataOperation.Create(context.Request, context.RequestAborted);

                var response = await streamsMetadata.Post(options, context.RequestAborted);

                await context.WriteHalResponse(response);
            };

        private static MidFunc GetStreamMetadata(StreamMetadataResource streamsMetadata)
            => async (context, next) =>
            {
                var options = new GetStreamMetadataOperation(context.Request);

                var response = await streamsMetadata.Get(options, context.RequestAborted);

                using(new OptionalHeadRequestWrapper(context))
                {
                    await context.WriteHalResponse(response);
                }
            };
    }
}