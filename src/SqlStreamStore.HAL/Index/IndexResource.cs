namespace SqlStreamStore.HAL.Index
{
    using System.Net.Http;

    internal class IndexResource : IResource
    {
        public HttpMethod[] Allowed { get; } =
        {
            HttpMethod.Get,
            HttpMethod.Head,
            HttpMethod.Options
        };
    }
}