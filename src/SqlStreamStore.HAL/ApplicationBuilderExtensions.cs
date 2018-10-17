namespace SqlStreamStore.HAL
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
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

        public static IApplicationBuilder UseAllowedMethods(this IApplicationBuilder builder, IResource resource)
            => builder.Use((context, next) =>
            {
                if(!resource.Allowed.Contains(new HttpMethod(context.Request.Method)))
                {
                    context.Response.StatusCode = 405;
                    return Task.CompletedTask;
                }

                return next();
            });

    }
}