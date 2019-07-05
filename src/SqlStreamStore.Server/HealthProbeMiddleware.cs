using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MidFunc = System.Func<
    Microsoft.AspNetCore.Http.HttpContext,
    System.Func<System.Threading.Tasks.Task>,
    System.Threading.Tasks.Task
>;

namespace SqlStreamStore.Server
{
    internal static class HealthProbeMiddleware
    {
        public static IApplicationBuilder UseHealthProbe(
            this IApplicationBuilder builder, IReadonlyStreamStore streamStore)
            => builder
                .Use(GetAndHeadOnly)
                .UseRouter(router => router
                .MapMiddlewareGet("ready", inner => inner.Use(Ready(streamStore)))
                .MapMiddlewareGet("live", inner => inner.Use(Live)));

        private static MidFunc Live => (context, next) =>
        {
            context.Response.StatusCode = 204;
            return Task.CompletedTask;
        };

        private static MidFunc Ready(IReadonlyStreamStore streamStore) => async (context, next) =>
        {
            try
            {
                await streamStore.ReadHeadPosition(context.RequestAborted);
            }
            catch
            {
                context.Response.StatusCode = 503;
                return;
            }

            context.Response.StatusCode = 204;
        };

        private static MidFunc GetAndHeadOnly => (context, next) =>
        {
            switch (context.Request.Method)
            {
                case "HEAD":
                    context.Request.Method = "GET";
                    return next();
                case "GET":
                    return next();
                default:
                    context.Response.StatusCode = 405;
                    return Task.CompletedTask;
            }
        };
    }
}