﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SqlStreamStore.HAL;
using SqlStreamStore.Server.Browser;

namespace SqlStreamStore.Server
{
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal class SqlStreamStoreServerStartup : IStartup
    {
        private readonly IStreamStore _streamStore;
        private readonly SqlStreamStoreMiddlewareOptions _options;

        public SqlStreamStoreServerStartup(
            IStreamStore streamStore,
            SqlStreamStoreMiddlewareOptions options)
        {
            _streamStore = streamStore;
            _options = options;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services) => services
            .AddResponseCompression(options => options.MimeTypes = new[] { "application/hal+json" })
            .AddRouting()
            .BuildServiceProvider();

        public void Configure(IApplicationBuilder app) => app
            .UseResponseCompression()
            .Use(VaryAccept)
            .UseSqlStreamStoreBrowser()
            .UseSqlStreamStoreHal(_streamStore, _options);

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