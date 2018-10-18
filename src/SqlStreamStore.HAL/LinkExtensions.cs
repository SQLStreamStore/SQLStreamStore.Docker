namespace SqlStreamStore.HAL
{
    internal static class LinkExtensions
    {
        public static Links Index(this Links links) =>
            links.Add(Constants.Relations.Index, string.Empty, "Index");

        public static Links Find(this Links links)
            => links.Add(Constants.Relations.Find, "streams/{streamId}", "Find a Stream");
    }
}