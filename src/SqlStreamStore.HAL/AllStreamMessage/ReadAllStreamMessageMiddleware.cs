namespace SqlStreamStore.HAL.AllStreamMessage
{
    using System.Net.Http;
    using Microsoft.AspNetCore.Builder;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class ReadAllStreamMessageMiddleware
    {
        public static IApplicationBuilder UseAllStreamMessage(
            this IApplicationBuilder builder,
            IStreamStore streamStore)
        {
            var allStreamMessages = new AllStreamMessageResource(streamStore);

            return builder
                .MapWhen(HttpMethod.Get, inner => inner.Use(GetStreamMessage(allStreamMessages)))
                .UseAllowedMethods(allStreamMessages);
        }

        private static MidFunc GetStreamMessage(AllStreamMessageResource allStreamMessages) => async (context, next) =>
        {
            var response = await allStreamMessages.Get(
                new ReadAllStreamMessageOperation(context.Request),
                context.RequestAborted);

            await context.WriteResponse(response);
        };

    }
}