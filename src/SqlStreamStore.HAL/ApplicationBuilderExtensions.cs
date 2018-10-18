namespace SqlStreamStore.HAL
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder MapWhen(
            this IApplicationBuilder builder,
            HttpMethod method,
            Action<IApplicationBuilder> configure) => builder.MapWhen(
            context => string.Equals(
                context.Request.Method,
                method.ToString(),
                StringComparison.OrdinalIgnoreCase),
            configure);

        public static IApplicationBuilder UseAllowedMethods<TResource>(this IApplicationBuilder builder, TResource resource)
            where TResource : IResource
        {
            var allowed = ResourceMethods.Discover<TResource>();

            Task AllowedMethods(HttpContext context, Func<Task> next)
            {
                if(!allowed.Contains(new HttpMethod(context.Request.Method)))
                {
                    context.Response.StatusCode = 405;
                    return Task.CompletedTask;
                }

                return next();
            }


            return builder
                .Use(Options(allowed))
                .Use(AllowedMethods);
        }

        private static MidFunc Options(HttpMethod[] allowed) => (context, next) =>
        {
            context.SetStandardCorsHeaders(allowed);

            return Task.CompletedTask;
        };
    }
}