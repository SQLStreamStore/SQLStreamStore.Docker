namespace SqlStreamStore.HAL
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    internal static class Constants
    {
        public static class Headers
        {
            public const string ExpectedVersion = "SSS-ExpectedVersion";
            public const string HeadPosition = "SSS-HeadPosition";
            public const string MessageId = "SSS-MessageId";
            public const string Location = "Location";
            
            public static class ContentTypes
            {
                public const string Json = "application/json";
                public const string HalJson = "application/hal+json";
                public const string Any = "*/*";
            }
        }

        public static class Relations
        {
            public const string Self = "self";
            public const string First = "first";
            public const string Previous = "previous";
            public const string Next = "next";
            public const string Last = "last";
            public const string Feed = "streamStore:feed";
            public const string Message = "streamStore:message";
            public const string Metadata = "streamStore:metadata";
        }

        public static class Streams
        {
            public const string All = "stream";
            public const string Metadata = "metadata";
        }
        
        public static IReadOnlyDictionary<int, string> ReasonPhrases { get; }
            = new ReadOnlyDictionary<int, string>(new Dictionary<int, string>
            {
                [200] = "OK",
                [201] = "Created",
                [307] = "Moved Temporarily",
                [400] = "Bad Request",
                [404] = "Not Found",
                [405] = "Method Not Allowed",
                [409] = "Conflict"
            });

        public static class ReadDirection
        {
            public const int Forwards = 1;
            public const int Backwards = -1;
        }
        
        public const int MaxCount = 20;
    }
}