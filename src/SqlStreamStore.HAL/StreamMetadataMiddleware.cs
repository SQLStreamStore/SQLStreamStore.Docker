namespace SqlStreamStore.HAL
{
    using Microsoft.Owin;
    using Microsoft.Owin.Builder;
    using Owin;
    using SqlStreamStore.HAL.Resources;
    using MidFunc = System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task
        >, System.Func<System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task>
    >;

    internal static class StreamMetadataMiddleware
    {
        public static MidFunc UseStreamStore(IStreamStore streamStore)
        {
            var streamsMetadata = new StreamMetadataResource(streamStore);

            var builder = new AppBuilder()
                .MapWhen(IsSetStreamMetadata, inner => inner.Use(SetStreamMetadata(streamsMetadata)))
                .MapWhen(IsGetStreamMetadata, inner => inner.Use(GetStreamMetadata(streamsMetadata)));

            return next =>
            {
                builder.Run(ctx => next(ctx.Environment));

                return builder.Build();
            };
        }

        private static bool IsSetStreamMetadata(IOwinContext context)
            => context.IsPost() && context.Request.Path.IsStreamMetadata();

        private static bool IsGetStreamMetadata(IOwinContext context)
            => context.IsGetOrHead() && context.Request.Path.IsStreamMetadata();

        private static MidFunc SetStreamMetadata(StreamMetadataResource streamsMetadata)
            => next => async env =>
            {
                var context = new OwinContext(env);

                var options = await SetStreamMetadataOperation.Create(context.Request, context.Request.CallCancelled);

                var response = await streamsMetadata.SetStreamMetadata(options, context.Request.CallCancelled);

                await context.WriteHalResponse(response);
            };

        private static MidFunc GetStreamMetadata(StreamMetadataResource streamsMetadata)
            => next => async env =>
            {
                var context = new OwinContext(env);

                var options = new GetStreamMetadataOperation(context.Request);

                var response = await streamsMetadata.GetStreamMetadata(options, context.Request.CallCancelled);

                using(new OptionalHeadRequestWrapper(context))
                {
                    await context.WriteHalResponse(response);
                }
            };
    }
}