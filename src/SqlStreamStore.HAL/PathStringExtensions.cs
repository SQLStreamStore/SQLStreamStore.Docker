namespace SqlStreamStore.HAL
{
    using Microsoft.Owin;

    internal static class PathStringExtensions
    {
        public static bool IsAllStream(this PathString requestPath) 
            => !requestPath.HasValue;

        public static bool IsAllStreamMessage(this PathString requestPath) 
            => long.TryParse(requestPath.Value?.Remove(0, 1), out var _);

        public static bool IsStream(this PathString requestPath) 
            => requestPath.Value?.Length > 1;

        public static bool IsStreamMessage(this PathString requestPath) 
            => requestPath.Value?.Split('/')?.Length == 3;
    }
}