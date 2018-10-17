namespace SqlStreamStore.HAL.StreamMetadata
{
    internal static class LinkExtensions
    {
        public static TheLinks MetadataNavigation(this TheLinks links, GetStreamMetadataOperation operation)
            => links.MetadataNavigation(operation.StreamId);

        public static TheLinks MetadataNavigation(this TheLinks links, SetStreamMetadataOperation operation)
            => links.MetadataNavigation(operation.StreamId);

        private static TheLinks MetadataNavigation(this TheLinks links, string streamId)
            => links.Add(Constants.Relations.Metadata, $"streams/{streamId}/{Constants.Streams.Metadata}").Self()
                .Add(Constants.Relations.Feed, $"streams/{streamId}");
    }
}