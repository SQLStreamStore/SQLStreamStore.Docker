namespace SqlStreamStore.HAL
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
            => builder.MapWhen(IsIndex, inner => inner.Use(Index));

        private static bool IsIndex(HttpContext context)
            => (context.Request.Path.Value ?? "/") == "/";

        private static MidFunc Index => (context, next) =>
        {
            var response = new Response(new HALResponse(null)
                .AddLinks(new Link(Constants.Relations.Feed, "stream"))
                .AddLinks(new Link(Constants.Relations.Self, string.Empty))
                .AddLinks(Links.Index(string.Empty))
                .AddLinks(Links.Find("streams/{streamId}")));

            return context.WriteResponse(response);
        };
    }
}