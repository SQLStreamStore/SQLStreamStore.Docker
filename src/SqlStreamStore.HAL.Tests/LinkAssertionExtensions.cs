namespace SqlStreamStore.HAL.Tests
{
    using Shouldly;

    internal static class LinkAssertionExtensions
    {
        public static void ShouldLink(this Resource resource, string rel, string href, string title = null)
            => resource.Links[rel]
                .ShouldHaveSingleItem()
                .ShouldBe(new Link
                {
                    Href = href,
                    Rel = rel,
                    Title = title
                });
    }
}