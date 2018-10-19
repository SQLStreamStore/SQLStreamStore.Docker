namespace SqlStreamStore.HAL
{
    using System;
    using System.Collections.Generic;
    using Halcyon.HAL;

    internal class Links
    {
        private readonly List<(string rel, string href, string title)> _links;
        private readonly string _relativePathToRoot;

        public static Links RootedAt(string relativePathToRoot) => new Links(relativePathToRoot);

        private Links(string relativePathToRoot)
        {
            if(!String.IsNullOrEmpty(relativePathToRoot))
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

            _relativePathToRoot = relativePathToRoot ?? String.Empty;
            _links = new List<(string rel, string href, string title)>();
        }

        public Links Add(string rel, string href, string title = null)
        {
            if(rel == null)
                throw new ArgumentNullException(nameof(rel));
            if(href == null)
                throw new ArgumentNullException(nameof(href));

            _links.Add((rel, href, title));
            return this;
        }

        public Links Self()
        {
            var link = _links[_links.Count - 1];

            return Add(Constants.Relations.Self, link.href, link.title);
        }

        public Links AddSelf(string rel, string href, string title = null)
            => Add(rel, href, title)
                .Add(Constants.Relations.Self, href, title);

        public Link[] ToHalLinks()
        {
            var links = new Link[_links.Count + 1];

            for(var i = 0; i < _links.Count; i++)
            {
                var (rel, href, title) = _links[i];
                var resolvedHref = $"{_relativePathToRoot}{href}";

                links[i] = new Link(rel, resolvedHref, title, replaceParameters: false);
            }

            links[_links.Count] = new Link(
                Constants.Relations.Curies,
                $"{_relativePathToRoot}docs/{{rel}}",
                replaceParameters: false)
            {
                Name = Constants.Relations.StreamStorePrefix
            };

            return links;
        }

        public static implicit operator Link[](Links links) => links.ToHalLinks();

        private static string FormatLink(
            string baseAddress,
            string direction,
            int maxCount,
            long position,
            bool prefetch)
            => $"{baseAddress}?d={direction}&m={maxCount}&p={position}&e={(prefetch ? 1 : 0)}";

        public static string FormatForwardLink(string baseAddress, int maxCount, long position, bool prefetch)
            => FormatLink(baseAddress, "f", maxCount, position, prefetch);

        public static string FormatBackwardLink(string baseAddress, int maxCount, long position, bool prefetch)
            => FormatLink(baseAddress, "b", maxCount, position, prefetch);
    }
}