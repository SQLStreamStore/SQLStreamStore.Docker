namespace SqlStreamStore.HAL.Docs
{
    using Microsoft.AspNetCore.Builder;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class DocsMiddleware
    {
        public static IApplicationBuilder UseDocs(
            this IApplicationBuilder builder,
            params IResource[] resources) 
            => builder.Use(Docs(new DocsResource(resources)));

        private static MidFunc Docs(DocsResource resource) => (context, next) =>
        {
            Response response;
            if(!context.Request.Path.StartsWithSegments("/docs", out var rel)
                || (response = resource.Get(rel.Value.Remove(0, 1))) == null)
                return next();
            return context.WriteResponse(response);
        };
    }
}