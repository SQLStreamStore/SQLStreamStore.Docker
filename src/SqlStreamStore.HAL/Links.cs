namespace SqlStreamStore.HAL
{
    using System;
    using System.Collections.Generic;
    using Halcyon.HAL;

    internal static class Links
    {
        public static Link Find(string href) =>
            new Link(Constants.Relations.Find, href, "Find a Stream", replaceParameters: false);

        public static Link Index(string href) =>
            new Link(Constants.Relations.Index, href, "Index", replaceParameters: false);
    }

    internal static class LinkExtensions
    {
        public static TheLinks Index(this TheLinks links) =>
            links.Add(Constants.Relations.Index, string.Empty, "Index");

        public static TheLinks Find(this TheLinks links)
            => links.Add(Constants.Relations.Find, "streams/{streamId}", "Find a Stream");
    }

    internal class TheLinks
    {
        private readonly List<(string rel, string href, string title)> _links;
        private readonly string _relativePathToRoot;

        public static TheLinks RootedAt(string relativePathToRoot) => new TheLinks(relativePathToRoot);

        private TheLinks(string relativePathToRoot)
        {
            if(!string.IsNullOrEmpty(relativePathToRoot))
            {
                if(!relativePathToRoot.StartsWith(".."))
                {
                    throw new ArgumentException("Non-empty relative path to root must start with '..'",
                        nameof(relativePathToRoot));
                }

                if(!relativePathToRoot.EndsWith("/"))
                {
                    throw new ArgumentException("Non-empty relative path to root must end with '/'",
                        nameof(relativePathToRoot));
                }
            }

            _relativePathToRoot = relativePathToRoot ?? string.Empty;
            _links = new List<(string rel, string href, string title)>();
        }

        public TheLinks Add(string rel, string href, string title = null)
        {
            if(rel == null)
                throw new ArgumentNullException(nameof(rel));
            if(href == null)
                throw new ArgumentNullException(nameof(href));

            _links.Add((rel, href, title));
            return this;
        }

        public TheLinks Self()
        {
            var link = _links[_links.Count - 1];

            return Add(Constants.Relations.Self, link.href, link.title);
        }

        public TheLinks AddSelf(string rel, string href, string title = null)
            => Add(rel, href, title)
                .Add(Constants.Relations.Self, href, title);

        public Link[] ToHalLinks()
        {
            var links = new Link[_links.Count];

            for(var i = 0; i < _links.Count; i++)
            {
                var (rel, href, title) = _links[i];
                var resolvedHref = $"{_relativePathToRoot}{href}";

                links[i] = new Link(rel, resolvedHref, title, replaceParameters: false);
            }

            return links;
        }

        public static implicit operator Link[](TheLinks theLinks) => theLinks.ToHalLinks();
    }
}