namespace SqlStreamStore.HAL
{
    internal static class Constants
    {
        public static class MediaTypes
        {
            public const string TextMarkdown = "text/markdown; charset=UTF-8";
            public const string HalJson = "application/hal+json";
            public const string Any = "*/*";
        }

        public static class Headers
        {
            public const string ExpectedVersion = "SSS-ExpectedVersion";
            public const string HeadPosition = "SSS-HeadPosition";
            public const string MessageId = "SSS-MessageId";
            public const string Location = "Location";
            public const string ETag = "ETag";
            public const string IfNoneMatch = "If-None-Match";
            public const string CacheControl = "Cache-Control";
        }

        public static class Relations
        {
            public const string StreamStorePrefix = "streamStore";
            public const string Curies = "curies";
            public const string Self = "self";
            public const string First = "first";
            public const string Previous = "previous";
            public const string Next = "next";
            public const string Last = "last";
            public const string Index = StreamStorePrefix + ":index";
            public const string Feed = StreamStorePrefix + ":feed";
            public const string Message = StreamStorePrefix + ":message";
            public const string Metadata = StreamStorePrefix + ":metadata";
            public const string AppendToStream = StreamStorePrefix + ":append";
            public const string Delete = StreamStorePrefix + ":delete";
            public const string Find = StreamStorePrefix + ":find";
        }

        public static class Streams
        {
            public const string All = "stream";
            public const string Metadata = "metadata";
        }

        public static class ReadDirection
        {
            public const int Forwards = 1;
            public const int Backwards = -1;
        }

        public const int MaxCount = 20;
    }
}