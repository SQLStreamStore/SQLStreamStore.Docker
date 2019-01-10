using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace SqlStreamStore.Server
{
    internal static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseStartup(this IWebHostBuilder builder, IStartup startup)
            => builder
                .ConfigureServices(services => services.AddSingleton(startup));
    }
}