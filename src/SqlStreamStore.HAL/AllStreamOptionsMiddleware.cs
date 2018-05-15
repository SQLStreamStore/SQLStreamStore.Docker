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

    internal static class AllStreamOptionsMiddleware
    {
        public static IApplicationBuilder UseAllStreamOptions(this IApplicationBuilder builder, IStreamStore streamStore)
        {
            var allStream = new AllStreamResource(streamStore);
            var allStreamMessages = new AllStreamMessageResource(streamStore);

            return builder
                .MapWhen(IsAllStream, ConfigureOptions(allStream))
                .MapWhen(IsAllStreamMessage, ConfigureOptions(allStreamMessages));
        }

        private static bool IsAllStream(HttpContext context)
            => context.Request.Path.IsAllStream();

        private static bool IsAllStreamMessage(HttpContext context)
            => context.Request.Path.IsAllStreamMessage();

        private static Action<IApplicationBuilder> ConfigureOptions(IResource resource)
            => builder => builder.Use(Options(resource));

        private static MidFunc Options(IResource resource) => (context, next) =>
        {
            context.SetStandardCorsHeaders(resource.Options);

            return Task.CompletedTask;
        };
    }
}