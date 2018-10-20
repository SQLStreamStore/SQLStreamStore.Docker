namespace SqlStreamStore.HAL.Index
{
    using Halcyon.HAL;

    internal class IndexResource : IResource
    {
        public SchemaSet Schema { get; }

        public Response Get() => new HalJsonResponse(new HALResponse(null)
            .AddLinks(
                Links
                    .RootedAt(string.Empty)
                    .Index().Self()
                    .Find()
                    .Add(Constants.Relations.Feed, Constants.Streams.All)));
    }
}