using Microsoft.Owin.Builder;
using Microsoft.Owin.Hosting;
using Owin;
using SqlStreamStore.Streams;
using System;
using System.Web.Http;
using MidFunc = System.Func<
    System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>,
    System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>>;

namespace SqlStreamStore.HAL
{
    class Program
    {
        static void Main(string[] args)
        {
            var streamStore = new InMemoryStreamStore();
            var messages = SeedData.Get(1000);

            streamStore.AppendToStream("SomeStream", ExpectedVersion.Any, messages);

            var settings = new HALSettings { Url = "Stream" };
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
                config.Routes.MapHttpRoute(
                   name: "SqlStreamStoreHAL",
                   routeTemplate: settings.Url + "/{checkpoint}",
                   defaults: new { controller = "SqlStreamStoreHAL", checkpoint = RouteParameter.Optional , action = "index" }
               );


                var appBuilder = new AppBuilder();
                appBuilder.UseWebApi(config);
                //appBuilder.Run(context =>
                //{
                //    context.Response.ContentType = "text/plain";

                //    string output = string.Format(
                //        "I'm running on {0} nFrom assembly {1}",
                //        Environment.OSVersion,
                //        System.Reflection.Assembly.GetEntryAssembly().FullName
                //    );

                //    return context.Response.WriteAsync(output);
                //});
                return appBuilder.Build();
            };
        }
    }

    public class SqlStreamStoreHALController : ApiController
    {
        [HttpGet]
        public string Index()
        {
            return "test";
        }
    }

    internal class HALSettings
    {
        public string Url { get; set; }
    }
}
