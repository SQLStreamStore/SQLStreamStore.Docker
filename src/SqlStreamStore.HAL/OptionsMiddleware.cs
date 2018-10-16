namespace SqlStreamStore.HAL
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using SqlStreamStore.HAL.Resources;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class ResourceOptionsMiddleware
    {
        public static IApplicationBuilder UseOptions(this IApplicationBuilder builder, IResource resource)
            => builder.Use(Options(resource));

        private static MidFunc Options(IResource resource) => (context, next) =>
        {
            context.SetStandardCorsHeaders(resource.Allowed);

            return Task.CompletedTask;
        };
    }
}