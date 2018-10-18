namespace SqlStreamStore.HAL.Index
{
    using Halcyon.HAL;

    internal class IndexResource : IResource
    {
        public Response Get() => new Response(new HALResponse(null)
            .AddLinks(
                Links
                    .RootedAt(string.Empty)
                    .Index().Self()
                    .Find()
                    .Add(Constants.Relations.Feed, "stream")));
    }
}