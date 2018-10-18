namespace SqlStreamStore.HAL
{
    internal static class LinkExtensions
    {
        public static TheLinks Index(this TheLinks links) =>
            links.Add(Constants.Relations.Index, string.Empty, "Index");

        public static TheLinks Find(this TheLinks links)
            => links.Add(Constants.Relations.Find, "streams/{streamId}", "Find a Stream");
    }
}