namespace SqlStreamStore.HAL
{
    using Microsoft.Owin;

    internal static class PathStringExtensions
    {
        public static bool IsAllStream(this PathString requestPath) 
            => !requestPath.HasValue;

        public static bool IsAllStreamMessage(this PathString requestPath) 
            => long.TryParse(requestPath.Value?.Remove(0, 1), out _);

        public static bool IsStream(this PathString requestPath) 
            => requestPath.Value?.Split('/').Length == 2;

        public static bool IsStreamMessage(this PathString requestPath)
        {
            var segments = requestPath.Value?.Split('/');
            
            return segments?.Length == 3 && int.TryParse(segments[2], out _);
        }

        public static bool IsStreamMetadata(this PathString requestPath)
        {
            var segments = requestPath.Value?.Split('/');
            
            return segments?.Length == 3 && segments[2] == Constants.Streams.Metadata;
        }
    }
}