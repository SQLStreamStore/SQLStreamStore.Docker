using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using SqlStreamStore.HAL;
using SqlStreamStore.Server.Browser;
using MidFunc = System.Func<
    Microsoft.AspNetCore.Http.HttpContext,
    System.Func<System.Threading.Tasks.Task>,
    System.Threading.Tasks.Task
>;

namespace SqlStreamStore.Server
{
    internal class SqlStreamStoreServerStartup : IStartup
    {
        private static readonly IEnumerable<string> s_CompressableMimeTypes = ResponseCompressionDefaults
            .MimeTypes.Concat(new[]
            {
                "application/hal+json",
                "text/markdown"
            });

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
            .AddResponseCompression(options => options.MimeTypes = s_CompressableMimeTypes)
            .AddSqlStreamStoreHal()
            .BuildServiceProvider();

        public void Configure(IApplicationBuilder app) => app
            .Map("/health", inner => inner.UseHealthProbe(_streamStore))
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