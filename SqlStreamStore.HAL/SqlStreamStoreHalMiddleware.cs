using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin.Builder;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Owin;

namespace SqlStreamStore.HAL
{
    internal static class SqlStreamStoreHalMiddleware
    {
        public static void UseSqlStreamStoreHal(this IAppBuilder app, SqlStreamStoreHalSettings settings)
        {
            app.Use(Handle(settings));
        }

        private static Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>> Handle(SqlStreamStoreHalSettings settings)
        {
            return next =>
            {
                var appBuilder = new AppBuilder();

                appBuilder
                    .UseNancy(configuration => configuration.Bootstrapper = new Bootstrapper(settings))
                    .Run(ctx => next(ctx.Environment));

                return appBuilder.Build();
            };
        }
    }

    internal class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly SqlStreamStoreHalSettings _settings;

        public Bootstrapper(SqlStreamStoreHalSettings settings)
        {
            _settings = settings;
        }

        protected override IEnumerable<ModuleRegistration> Modules => new List<ModuleRegistration>
        {
            new ModuleRegistration(typeof(SqlStreamStoreHalModule))
        };

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            container.Register(_settings);
            base.ApplicationStartup(container, pipelines);
        }
    }
    
    public class SqlStreamStoreHalSettings
    {
        public IReadonlyStreamStore Store { get; set; }

        public string BaseUrl { get; set; }

        public int PageSize { get; set; }
    }
}