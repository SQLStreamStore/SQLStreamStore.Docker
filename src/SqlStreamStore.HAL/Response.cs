namespace SqlStreamStore.HAL
{
    using Halcyon.HAL;

    public class Response
    {
        public HALResponse Hal { get; }
        public int StatusCode { get; }

        public Response(HALResponse hal, int statusCode = 200)
        {
            Hal = hal;
            StatusCode = statusCode;
        }
    }
}