﻿namespace SqlStreamStore.HAL.DevServer
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;
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

        public IServiceProvider ConfigureServices(IServiceCollection services) => services
            .AddResponseCompression(options => options.MimeTypes = new[] { "application/hal+json" })
            .BuildServiceProvider();

        public void Configure(IApplicationBuilder app) => app
            .UseResponseCompression()
            .Use(VaryAccept)
            .Use(CatchAndDisplayErrors)
            .UseSqlStreamStoreBrowser()
            .UseSqlStreamStoreHal(_streamStore);

        private static MidFunc CatchAndDisplayErrors => async (context, next) =>
        {
            try
            {
                await next();
            }
            catch(Exception ex)
            {
                Log.Warning(ex, "Error during request.");
            }
        };

        private static MidFunc VaryAccept => (context, next) =>
        {
            Task Vary()
            {
                context.Response.Headers.AppendCommaSeparatedValues("Vary", "Accept");

                return Task.CompletedTask;
            }

            context.Response.OnStarting(Vary);

            return next();
        };
    }
}