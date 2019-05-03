using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Serilog;

namespace SqlStreamStore.Server.Browser
{
    internal static class SqlStreamStoreBrowserMiddleware
    {
        public static IApplicationBuilder UseSqlStreamStoreBrowser(
            this IApplicationBuilder builder,
            Type rootType = default)
        {
            rootType = rootType ?? typeof(SqlStreamStoreBrowserMiddleware);
            var sqlStreamStoreBrowserFileProvider = new EmbeddedFileProvider(
                rootType.Assembly,
                rootType.Namespace);

            var staticFiles = rootType.Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(rootType.Namespace));

            Log.Debug(
                "The following embedded resources were found and will be served as static content: {staticFiles}",
                string.Join(", ", staticFiles));

            return builder.Use(IndexPage).UseStaticFiles(new StaticFileOptions
            {
                FileProvider = sqlStreamStoreBrowserFileProvider
            });

            Task IndexPage(HttpContext context, Func<Task> next)
            {
                if (!GetAcceptHeaders(context.Request).Contains("text/html"))
                {
                    return TryRedirectStaticContent(context, next);
                }

                context.Request.Path = new PathString("/index.html");
                return next();
            }
        }

        private static string[] GetAcceptHeaders(HttpRequest contextRequest)
            => Array.ConvertAll(
                contextRequest.Headers.GetCommaSeparatedValues("Accept"),
                value => MediaTypeWithQualityHeaderValue.TryParse(value, out var header)
                    ? header.MediaType
                    : null);

        private static Task TryRedirectStaticContent(HttpContext context, Func<Task> next)
        {
            if (!context.Request.Path.HasValue)
            {
                return next();
            }

            var requestPieces = context.Request.Path.Value.Split('/');

            for (var i = 2; i < requestPieces.Length; i++)
            {
                if (requestPieces[i] != "static")
                {
                    continue;
                }

                var staticFilePath = new string[requestPieces.Length - i];
                for (var j = i; j < requestPieces.Length; j++)
                {
                    staticFilePath[j - i] = requestPieces[j];
                }

                context.Response.StatusCode = 308;
                context.Response.Headers["Location"] =
                    $"{string.Join("/", Enumerable.Repeat("..", i + 1))}/{string.Join("/", staticFilePath)}";
                return Task.CompletedTask;
            }

            return next();
        }
    }
}