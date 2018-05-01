namespace SqlStreamStore.HAL.Resources
{
    using System.Net.Http;

    internal interface IResource
    {
        HttpMethod[] Options { get; }
    }
}