namespace SqlStreamStore.HAL.DevServer
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Primitives;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal class DevServerStartup : IStartup
    {
        private readonly IStreamStore _streamStore;
        private readonly HttpClient _httpClient;

        public DevServerStartup(IStreamStore streamStore)
        {
            _streamStore = streamStore;
            _httpClient = new HttpClient();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services) => services
            .AddResponseCompression(options => options.MimeTypes = new[] { "application/hal+json" })
            .BuildServiceProvider();

        public void Configure(IApplicationBuilder app) => app
            .UseResponseCompression()
            .Use(CatchAndDisplayErrors)
            .Use(AllowAllOrigins)
            .Use(SqlStreamStreamBrowserJavascript)
            .Use(SqlStreamStreamBrowserHtml)
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

        private static string[] GetAcceptHeaders(HttpRequest contextRequest)
            => Array.ConvertAll(
                contextRequest.Headers.GetCommaSeparatedValues("Accept"),
                value => MediaTypeWithQualityHeaderValue.TryParse(value, out var header)
                    ? header.MediaType
                    : null);

        private MidFunc SqlStreamStreamBrowserJavascript => (context, next)
            => context.Request.Path.Value?.EndsWith(".js") ?? false
                ? ForwardToClientDevServer(
                    context,
                    $"{context.Request.PathBase.ToUriComponent()}{context.Request.Path.ToUriComponent()}")
                : next();

        private MidFunc SqlStreamStreamBrowserHtml => (context, next)
            => GetAcceptHeaders(context.Request)
                .Any(header => header == "text/html")
                ? ForwardToClientDevServer(context, context.Request.PathBase.ToUriComponent())
                : next();

        private async Task ForwardToClientDevServer(HttpContext context, string path)
        {
            using(var request = new HttpRequestMessage(
                new HttpMethod(context.Request.Method),
                new UriBuilder
                {
                    Port = 3000,
                    Host = "localhost",
                    Path = path,
                    Query = context.Request.QueryString.ToUriComponent()
                }.Uri))
            using(var response = await _httpClient.SendAsync(request))
            using(var stream = await response.Content.ReadAsStreamAsync())
            {
                context.Response.StatusCode = (int) response.StatusCode;
                foreach(var header in response.Headers.Concat(response.Content.Headers))
                {
                    context.Response.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));
                }

                await stream.CopyToAsync(context.Response.Body, 8192, context.RequestAborted);
            }
        }
    }
}