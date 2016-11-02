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
using System.Text;
using System.Linq;
using Newtonsoft.Json;
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
                Url = "Something",
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

                config.Routes.MapHttpRoute(
                   name: "SqlStreamStoreHAL",
                   routeTemplate: settings.Url + "/stream/{direction}/{position}",
                   defaults: new { controller = "SqlStreamStoreHAL", position = RouteParameter.Optional, action = "index", direction = "forwards" }
               );


                var appBuilder = new AppBuilder();
                appBuilder.UseWebApi(config);

                return appBuilder.Build();
            };
        }
    }

    public class SqlStreamStoreHALController : ApiController
    {
        private readonly Dictionary<string, int> _directionLookup = new Dictionary<string, int>
        {
            {"forwards", 1},
            {"backwards", -1}

        };

        private readonly IReadonlyStreamStore _store;

        private readonly int _pageSize;

        public SqlStreamStoreHALController(HALSettings settings)
        {
            _store = settings.Store;
            _pageSize = settings.PageSize;
        }

        [HttpGet]
        public IHttpActionResult Index(string direction, long? position = null)
        {
            var dir = _directionLookup[direction];

            var readAllPage = GetStream(position, dir);

            var response = HALResponse.Create(readAllPage.Messages, _pageSize, Request.RequestUri.AbsolutePath, dir);

            return Ok(response);
        }

        private ReadAllPage GetStream(long? position, int direction)
        {
            ReadAllPage readAllPage;

            if (direction == Direction.Forwards)
            {
                readAllPage = _store.ReadAllForwards(position ?? Position.Start, _pageSize).GetAwaiter().GetResult();
            }
            else
            {
                readAllPage = _store.ReadAllBackwards(position ?? Position.End, _pageSize).GetAwaiter().GetResult();
            }

            return readAllPage;
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

        public Object GetService(Type serviceType)
        {
            if (!_container.CanResolve(serviceType))
            {
                return null;
            }

            return _container.Resolve(serviceType);
        }

        public IEnumerable<Object> GetServices(Type serviceType)
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

        public string Url { get; set; }

        public int PageSize { get; set; }
    }
}
