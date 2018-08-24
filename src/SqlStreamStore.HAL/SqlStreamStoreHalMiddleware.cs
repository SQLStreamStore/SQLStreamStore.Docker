﻿namespace SqlStreamStore.HAL
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    public static class SqlStreamStoreHalMiddleware
    {
        private static MidFunc CaseSensitiveQueryStrings => (context, next) =>
        {
            if(context.Request.QueryString != QueryString.Empty)
            {
                var queryString = context.Request.QueryString;
                context.Request.Query = new CaseSensitiveQueryCollection(queryString);
                // Setting context.Request.Query mutates context.Request.QueryString.
                // This has the unfortunate side effect of turning ?a=1&b into ?a=1&b=.
                // so, replace with original context.Request.QueryString and call it a day.
                context.Request.QueryString = queryString;
            }

            return next();
        };

        private static MidFunc MethodsNotAllowed(params string[] methods) => (context, next) =>
        {
            if(!methods.Contains(context.Request.Method))
            {
                return next();
            }

            context.Response.StatusCode = 405;

            return Task.CompletedTask;
        };

        private static MidFunc AcceptHalJson => (context, next) =>
        {
            var accept = context.Request.GetAcceptHeaders();

            return accept.Any(header => header == Constants.Headers.ContentTypes.HalJson
                                        || header == Constants.Headers.ContentTypes.Any)
                ? next()
                : context.WriteHalResponse(new Response(new HALResponse(new
                    {
                        type = "Not Acceptable",
                        title = "Not Acceptable",
                        detail = $"The server only understands {Constants.Headers.ContentTypes.HalJson}."
                    }),
                    406));
        };

        private static MidFunc Index => (context, next) =>
        {
            if((context.Request.Path.Value ?? "/") != "/")
            {
                return next();
            }

            var response = new Response(new HALResponse(null)
                .AddLinks(new Link(Constants.Relations.Feed, "stream"))
                .AddLinks(new Link(Constants.Relations.Self, string.Empty))
                .AddLinks(new Link(Constants.Relations.Index, string.Empty)));

            return context.WriteHalResponse(response);
        };

        public static IApplicationBuilder UseSqlStreamStoreHal(
            this IApplicationBuilder builder,
            IStreamStore streamStore)
        {
            if(builder == null)
                throw new ArgumentNullException(nameof(builder));
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));

            return builder
                .Use(ExceptionHandlingMiddleware.HandleExceptions)
                .Use(CaseSensitiveQueryStrings)
                .Use(AcceptHalJson)
                .Use(Index)
                .Map("/stream", UseAllStream(streamStore))
                .Map("/streams", UseStream(streamStore));
        }

        private static Action<IApplicationBuilder> UseStream(IStreamStore streamStore)
            => builder => builder
                .MapWhen(IsOptions, inner => inner.UseStreamOptions(streamStore))
                .UseStreamMetadata(streamStore)
                .UseReadStream(streamStore)
                .UseAppendStream(streamStore)
                .UseDeleteStream(streamStore)
                .Use(MethodsNotAllowed("TRACE", "PATCH"));

        private static Action<IApplicationBuilder> UseAllStream(IStreamStore streamStore)
            => builder => builder
                .MapWhen(IsOptions, inner => inner.UseAllStreamOptions(streamStore))
                .UseReadAllStream(streamStore)
                .Use(MethodsNotAllowed("POST", "PUT", "DELETE", "TRACE", "PATCH"));

        private static bool IsOptions(HttpContext context) => context.IsOptions();
    }
}