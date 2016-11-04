using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Jil;
using Microsoft.Owin.Builder;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.IO;
using Nancy.TinyIoc;
using Owin;

namespace SqlStreamStore.HAL
{
    public static class SqlStreamStoreHalMiddleware
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
            container.Register<ISerializer, JilSerializer>();
            base.ApplicationStartup(container, pipelines);
        }
    }

    public class JilSerializer : ISerializer
    {
        private static readonly Options Options = new Options(
            excludeNulls: true,
            serializationNameFormat: SerializationNameFormat.CamelCase);

        public bool CanSerialize(string contentType)
        {
            return true;
        }

        public void Serialize<TModel>(string contentType, TModel model, Stream outputStream)
        {
            using (var output = new StreamWriter(new UnclosableStreamWrapper(outputStream)))
            {
                JSON.SerializeDynamic(model, output, Options);
            }
        }

        public IEnumerable<string> Extensions => new List<string> { "json" };
    }

    public class SqlStreamStoreHalSettings
    {
        public IReadonlyStreamStore Store { get; set; }

        public string BaseUrl { get; set; }

        public int PageSize { get; set; }
    }
}