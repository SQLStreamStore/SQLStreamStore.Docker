namespace SqlStreamStore.HAL
{
    using System.Net.Http;

    internal interface IResource
    {
        HttpMethod[] Allowed { get; }
    }
}