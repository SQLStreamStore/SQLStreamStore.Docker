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

            var settings = new HALSettings { Url = "", Store = streamStore };
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
                container.Register(settings.Store);


                config.DependencyResolver = new Resolver(container);

                config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();


                config.Routes.MapHttpRoute(
                   name: "SqlStreamStoreHAL",
                   routeTemplate: settings.Url + "stream/{position}",
                   defaults: new { controller = "SqlStreamStoreHAL", position = RouteParameter.Optional, action = "index" }
               );


                var appBuilder = new AppBuilder();
                appBuilder.UseWebApi(config);

                return appBuilder.Build();
            };
        }
    }

    public class SqlStreamStoreHALController : ApiController
    {
        private IReadonlyStreamStore _stream;

        public SqlStreamStoreHALController(IReadonlyStreamStore stream)
        {
            _stream = stream;
        }

        [HttpGet]
        public IHttpActionResult Index(long? position = null)
        {
            

            var readAllPage = _stream.ReadAllForwards(position ?? Position.Start, 20).GetAwaiter().GetResult();

            var stringBuilder = new StringBuilder();

            foreach (var e in readAllPage.Messages)
            {
                stringBuilder.AppendLine(e.JsonData + Environment.NewLine);
            }

            var response = new HALStream
            {
                Links = Links.CreateLinks(Request.RequestUri.AbsolutePath, position ?? Position.Start, 20),
                Embedded = new Embedded { Stream = readAllPage.Messages.Select(m => m).Cast<object>().ToList() }
            };

            return Ok(response);
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

    class HALSettings
    {
        public IReadonlyStreamStore Store { get; set; }
        public string Url { get; set; }
    }

    class HALStream
    {
        [JsonProperty(PropertyName = "_links")]
        public Links Links { get; set; }

        [JsonProperty(PropertyName = "_embedded")]
        public Embedded Embedded { get; set; }
    }
    class Links
    {
        public Link Self { get; set; }

        public Link Next { get; set; }

        public Link Prev { get; set; }

        public class Link
        {
            public string Href { get; private set; }

            public static Link Create(string href)
            {
                return new Link { Href = href };
            }
        }

        public static Links CreateLinks(string path, long position, long pageSize)
        {
            return new Links
            {
                Self = Link.Create(path + (position != Position.Start ? "?position=" + position : "")),
                Next = Link.Create(path + "?position=" + (position + 1 + pageSize)),
                Prev = Link.Create(position == Position.Start ? (string)null : path + "?position=" + (position - 1))
            };
        }
    }    

    class Embedded
    {
       public List<object> Stream { get; set; }
    }
}
