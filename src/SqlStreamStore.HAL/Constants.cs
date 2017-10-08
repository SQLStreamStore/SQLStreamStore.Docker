namespace SqlStreamStore.HAL
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    internal static class Constants
    {
        public static class Headers
        {
            public const string ExpectedVersion = "SSS-ExpectedVersion";

            public static class ContentTypes
            {
                public const string ProblemDetails = "application/problem+json";
                public const string Json = "application/json";
            }
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
    }
}