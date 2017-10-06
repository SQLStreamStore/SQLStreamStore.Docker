namespace SqlStreamStore.HAL
{
    using System;
    using Newtonsoft.Json.Linq;

    internal static class ExceptionExtensions
    {
        public static string ConvertToProblemDetails(this Exception ex)
            => JObject.FromObject(new
            {
                type = "WrongExpectedVersion",
                title = "Wrong expected version.",
                detail = ex.Message
            }).ToString();
    }
}