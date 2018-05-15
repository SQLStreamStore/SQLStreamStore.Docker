namespace SqlStreamStore.HAL.DevServer
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal class DevServerStartup : IStartup
    {
        private readonly IStreamStore _streamStore;

        public DevServerStartup(IStreamStore streamStore)
        {
            _streamStore = streamStore;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services) => services.BuildServiceProvider();

        public void Configure(IApplicationBuilder app) => app
            .Use(CatchAndDisplayErrors)
            .Use(AllowAllOrigins)
            .UseSqlStreamStoreHal(_streamStore);
        
        private static MidFunc CatchAndDisplayErrors => async (context, next) =>
        {
            try
            {
                await next();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        };

        // don't actually do this in production
        private static MidFunc AllowAllOrigins => (context, next) =>
        {
            context.Response.OnStarting(_ =>
                {
                    var response = (HttpResponse) _;
                    response.Headers["Access-Control-Allow-Origin"] = "*";

                    return Task.CompletedTask;
                },
                context.Response);

            return next();
        };

    }
}