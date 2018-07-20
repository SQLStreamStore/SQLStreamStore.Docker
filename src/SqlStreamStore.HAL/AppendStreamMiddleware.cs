namespace SqlStreamStore.HAL
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using SqlStreamStore.HAL.Resources;
    using MidFunc = System.Func<
        Microsoft.AspNetCore.Http.HttpContext,
        System.Func<System.Threading.Tasks.Task>,
        System.Threading.Tasks.Task
    >;

    internal static class AppendStreamMiddleware
    {
        public static IApplicationBuilder UseAppendStream(this IApplicationBuilder builder, IStreamStore streamStore)
        {
            var stream = new StreamResource(streamStore);
            
            return builder.MapWhen(IsStream, inner => inner.Use(AppendStream(stream)));
        }
        
        private static bool IsStream(HttpContext context)
            => context.IsPost() && context.Request.Path.IsStream();

        private static MidFunc AppendStream(StreamResource stream) => async (context, next) =>
        {
            var options = await AppendStreamOperation.Create(context.Request, context.RequestAborted);

            var response = await stream.Post(options, context.RequestAborted);

            await context.WriteHalResponse(response);
        };
    }
}