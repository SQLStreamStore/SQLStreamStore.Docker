using Microsoft.Owin.Builder;
using Microsoft.Owin.Hosting;
using Owin;
using SqlStreamStore.Streams;
using System;
using System.Web.Http;
using System.Web.Http.Dependencies;
using TinyIoC;
using MidFunc = System.Func<
    System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>,
    System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>>;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace SqlStreamStore.HAL
{
    class Program
    {
        static void Main(string[] args)
        {
            var streamStore = new InMemoryStreamStore();
            var messages = SeedData.Get(1000);

            streamStore.AppendToStream("SomeStream", ExpectedVersion.Any, messages);

            var settings = new HALSettings
            {
                Store = streamStore,
                PageSize = 20
            };

            var baseUrl = "http://localhost:8080";

            using (WebApp.Start(baseUrl, app => app.Use(HALMiddleware.Handle(settings))))
            {
                Console.WriteLine("Press Enter to quit.");
                Console.ReadKey();
            }
        }
    }

    internal static class HALMiddleware
    {
        public static MidFunc Handle(HALSettings settings)
        {
            return next =>
            {
                var config = new HttpConfiguration();

                var container = new TinyIoCContainer();
                container.Register(settings);

                config.DependencyResolver = new Resolver(container);
                config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

                var baseUrl = settings.BaseUrl == null ? "" : settings.BaseUrl + "/";

                config.Routes.MapHttpRoute(
                    "SqlStreamStoreHAL.Paged",
                   Path.Combine(baseUrl, "stream/{direction}/{position}"),
                   new { controller = "SqlStreamStoreHAL", position = RouteParameter.Optional, action = "index", direction = "forwards" }
                );

                config.Routes.MapHttpRoute(
                    "SqlStreamStoreHAL.Message",
                   Path.Combine(baseUrl, "streanMessage/{position}"),
                   new { controller = "SqlStreamStoreHAL", action = "message", position = RouteParameter.Optional }
                );

                var appBuilder = new AppBuilder();
                appBuilder.UseWebApi(config);

                return appBuilder.Build();
            };
        }
    }

    class Resolver : IDependencyResolver
    {
        private TinyIoCContainer _container;

        public Resolver(TinyIoCContainer container)
        {
            _container = container;
        }

        public IDependencyScope BeginScope()
        {
            return this;
        }

        public void Dispose()
        {
        }

        public object GetService(Type serviceType)
        {
            if (!_container.CanResolve(serviceType))
            {
                return null;
            }

            return _container.Resolve(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            if (!_container.CanResolve(serviceType))
            {
                return Enumerable.Empty<object>();
            }

            return _container.ResolveAll(serviceType);
        }
    }

    public class HALSettings
    {
        public IReadonlyStreamStore Store { get; set; }

        public string BaseUrl { get; set; }

        public int PageSize { get; set; }
    }
}
