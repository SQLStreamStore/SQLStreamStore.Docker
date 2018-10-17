namespace SqlStreamStore.HAL.Index
{
    using Halcyon.HAL;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class IndexMiddleware
    {
        public static IApplicationBuilder UseIndex(this IApplicationBuilder builder)
        {
            var index = new IndexResource();

            return builder
                .MapWhen(IsGetIndex, inner => inner.Use(Index()))
                .MapWhen(IsOptions, inner => inner.UseOptions(index))
                .MapWhen(IsIndex, inner => inner.UseAllowedMethods(index));
        }

        private static bool IsOptions(HttpContext context)
            => context.IsOptions() && context.Request.Path.IsIndex();

        private static bool IsGetIndex(HttpContext context)
            => context.IsGetOrHead() && context.Request.Path.IsIndex();

        private static bool IsIndex(HttpContext context)
            => context.Request.Path.IsIndex();

        private static MidFunc Index()
        {
            var response = new Response(new HALResponse(null)
                .AddLinks(
                    TheLinks
                        .RootedAt(string.Empty)
                        .Index().Self()
                        .Find()
                        .Add(Constants.Relations.Feed, "stream")));

            return (context, next) => context.WriteResponse(response);
        }
    }
}