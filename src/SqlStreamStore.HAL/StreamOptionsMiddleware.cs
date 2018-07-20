namespace SqlStreamStore.HAL
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using SqlStreamStore.HAL.Resources;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class StreamOptionsMiddleware
    {
        public static IApplicationBuilder UseStreamOptions(this IApplicationBuilder builder, IStreamStore streamStore)
        {
            var streams = new StreamResource(streamStore);
            var streamMessages = new StreamMessageResource(streamStore);
            var streamsMetadata = new StreamMetadataResource(streamStore);

            return builder
                .MapWhen(IsStream, ConfigureOptions(streams))
                .MapWhen(IsStreamMessage, ConfigureOptions(streamMessages))
                .MapWhen(IsStreamMetadata, ConfigureOptions(streamsMetadata));
        }

        private static bool IsStream(HttpContext context)
            => context.Request.Path.IsStream();

        private static bool IsStreamMessage(HttpContext context)
            => context.Request.Path.IsStreamMessage();

        private static bool IsStreamMetadata(HttpContext context)
            => context.IsOptions() && context.Request.Path.IsStreamMetadata();

        private static Action<IApplicationBuilder> ConfigureOptions(IResource resource)
            => builder => builder.Use(Options(resource));

        private static MidFunc Options(IResource resource) => (context, next) =>
        {
            context.SetStandardCorsHeaders(resource.Allowed);

            return Task.CompletedTask;
        };
    }
}