namespace SqlStreamStore.HAL
{
    using System.Collections.Generic;
    using Halcyon.HAL;

    public class Response
    {
        public HALResponse Hal { get; }
        public int StatusCode { get; }
        public IDictionary<string, string[]> Headers { get; }

        public Response(HALResponse hal, int statusCode = 200)
        {
            Hal = hal;
            StatusCode = statusCode;
            Headers = new Dictionary<string, string[]>();
        }
    }
}