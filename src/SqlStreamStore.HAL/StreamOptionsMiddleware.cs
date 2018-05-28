namespace SqlStreamStore.HAL
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using Microsoft.Owin.Builder;
    using Owin;
    using SqlStreamStore.HAL.Resources;
    using MidFunc = System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task
        >, System.Func<System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task>
    >;

    internal static class AllStreamOptionsMiddleware
    {
        public static MidFunc UseStreamStore(IStreamStore streamStore)
        {
            var allStream = new AllStreamResource(streamStore);
            var allStreamMessages = new AllStreamMessageResource(streamStore);

            var builder = new AppBuilder()
                .MapWhen(IsAllStream, ConfigureOptions(allStream))
                .MapWhen(IsAllStreamMessage, ConfigureOptions(allStreamMessages));

            return next =>
            {
                builder.Run(context => next(context.Environment));

                return builder.Build();
            };
        }

        private static bool IsAllStream(IOwinContext context)
            => context.Request.Path.IsAllStream();

        private static bool IsAllStreamMessage(IOwinContext context)
            => context.Request.Path.IsAllStreamMessage();

        private static Action<IAppBuilder> ConfigureOptions(IResource resource)
            => builder => builder.Use(Options(resource));

        private static MidFunc Options(IResource resource) => next => env =>
        {
            var context = new OwinContext(env);

            context.SetStandardCorsHeaders(resource.Options);

            return Task.CompletedTask;
        };
    }

    internal static class StreamOptionsMiddleware
    {
        public static MidFunc UseStreamStore(IStreamStore streamStore)
        {
            var streams = new StreamResource(streamStore);
            var streamMessages = new StreamMessageResource(streamStore);
            var streamsMetadata = new StreamMetadataResource(streamStore);

            var builder = new AppBuilder()
                .MapWhen(IsStream, ConfigureOptions(streams))
                .MapWhen(IsStreamMessage, ConfigureOptions(streamMessages))
                .MapWhen(IsStreamMetadata, ConfigureOptions(streamsMetadata));

            return next =>
            {
                builder.Run(context => next(context.Environment));

                return builder.Build();
            };
        }

        private static bool IsStream(IOwinContext context)
            => context.Request.Path.IsStream();

        private static bool IsStreamMessage(IOwinContext context)
            => context.Request.Path.IsStreamMessage();

        private static bool IsStreamMetadata(IOwinContext context)
            => context.IsOptions() && context.Request.Path.IsStreamMetadata();

        private static Action<IAppBuilder> ConfigureOptions(IResource resource)
            => builder => builder.Use(Options(resource));

        private static MidFunc Options(IResource resource) => next => env =>
        {
            var context = new OwinContext(env);

            context.SetStandardCorsHeaders(resource.Options);

            return Task.CompletedTask;
        };
    }
}