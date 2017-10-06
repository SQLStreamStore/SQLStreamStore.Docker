namespace SqlStreamStore.HAL
{
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
    }
}