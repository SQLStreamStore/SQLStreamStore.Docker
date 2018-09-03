namespace SqlStreamStore.HAL
{
    using Halcyon.HAL;

    internal static class Links
    {
        public static Link Find(string href) => new Link(Constants.Relations.Find, href, "Find a Stream", replaceParameters: false);
        public static Link Index(string href) => new Link(Constants.Relations.Index, href, "Index", replaceParameters: false);
    }
}