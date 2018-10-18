namespace SqlStreamStore.HAL.Index
{
    using System;
    using System.Threading.Tasks;
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
                .MapWhen(IsIndex, inner => inner.UseAllowedMethods(index));
        }

        private static bool IsGetIndex(HttpContext context)
            => context.IsGetOrHead() && context.Request.Path.IsIndex();

        private static bool IsIndex(HttpContext context)
            => context.Request.Path.IsIndex();

        private static MidFunc Index()
        {
            var resource = new IndexResource();
            
            var response = resource.Get();

            Task Index(HttpContext context, Func<Task> next) => context.WriteResponse(response);

            return Index;
        }
    }
}