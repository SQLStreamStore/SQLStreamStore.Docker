namespace SqlStreamStore.HAL
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
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
                method.Method,
                StringComparison.OrdinalIgnoreCase),
            configure);

        public static IApplicationBuilder UseAllowedMethods(this IApplicationBuilder builder, IResource resource)
        {
            var allowed = ResourceMethods.Discover(resource);

            var allowedMethodsHeaderValue = allowed.Aggregate(
                StringValues.Empty,
                (previous, method) => StringValues.Concat(previous, method.Method));

            var allowedHeadersHeaderValue = new StringValues(new[]
            {
                Constants.Headers.ContentType,
                Constants.Headers.XRequestedWith,
                Constants.Headers.Authorization
            });
            
            Task Options(HttpContext context, Func<Task> next)
            {
                context.Response.Headers.Append(
                    Constants.Headers.AccessControl.AllowMethods,
                    allowedMethodsHeaderValue);
                context.Response.Headers.Append(
                    Constants.Headers.AccessControl.AllowHeaders,
                    allowedHeadersHeaderValue);
                context.Response.Headers.Append(
                    Constants.Headers.AccessControl.AllowOrigin,
                    "*");
                
                return Task.CompletedTask;
            }

            Task AllowedMethods(HttpContext context, Func<Task> next)
            {
                if(!allowed.Contains(new HttpMethod(context.Request.Method)))
                {
                    context.Response.StatusCode = 405;
                    context.Response.Headers.Add(Constants.Headers.Allowed, allowedMethodsHeaderValue);
                    return Task.CompletedTask;
                }

                return next();
            }

            return builder
                .MapWhen(HttpMethod.Options, inner => inner.Use(Options))
                .Use(AllowedMethods);
        }
    }
}