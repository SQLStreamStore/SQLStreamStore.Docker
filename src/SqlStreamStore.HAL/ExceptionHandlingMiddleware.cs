﻿namespace SqlStreamStore.HAL
{
    using System;
    using System.Collections.Generic;
    using Halcyon.HAL;
    using Newtonsoft.Json;
    using SqlStreamStore.Streams;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;
    
    internal static class ExceptionHandlingMiddleware
    {
        private static readonly Func<Exception, Response> s_defaultExceptionHandler
            = ex => new Response(new HALResponse(new
                {
                    type = ex.GetType().Name,
                    title = "Internal Server Error",
                    detail = ex.Message
                }),
                500);
        
        private static readonly IDictionary<Type, Func<Exception, Response>> s_exceptionHandlers 
            = new Dictionary<Type, Func<Exception, Response>>
            {
                [typeof(WrongExpectedVersionException)] = ex => new Response(new HALResponse(new
                {
                    type = ex.GetType().Name,
                    title = "Wrong expected version.",
                    detail = ex.Message
                }), 409),
                [typeof(JsonException)] = ex => new Response(new HALResponse(new
                {
                    type = ex.GetType().Name,
                    title = "Bad format."
                }), 400),
                [typeof(InvalidAppendRequestException)] = ex => new Response(new HALResponse(new
                {
                    type = ex.GetType().Name,
                    title = "Bad format.",
                    detail = ex.Message
                }), 400),
                [typeof(Exception)] = s_defaultExceptionHandler
            };

        public static MidFunc HandleExceptions => async (context, next) =>
        {
            try
            {
                await next();
            }
            catch(Exception ex)
            {
                var exceptionType = ex.GetType();

                Func<Exception, Response> exceptionHandler = null;
                
                while(exceptionType != null)
                {
                    if(s_exceptionHandlers.TryGetValue(exceptionType, out exceptionHandler))
                    {
                        break;
                    }
                    
                    exceptionType = exceptionType.BaseType;
                }

                var response = (exceptionHandler ?? s_defaultExceptionHandler)(ex);

                await context.WriteHalResponse(response);
            }
        };
    }
}