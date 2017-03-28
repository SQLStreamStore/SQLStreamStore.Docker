namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class Resource
    {
        public Resource(dynamic state, Link[] links, Tuple<string, Resource>[] embedded)
        {
            State = state;
            Links = links.GroupBy(l => l.Rel).ToDictionary(g => g.Key, g => g.ToArray());
            Embedded = embedded.GroupBy(e => e.Item1).ToDictionary(g => g.Key, g => g.Select(e => e.Item2).ToArray());
        }

        public dynamic State { get; }
        public IReadOnlyDictionary<string, Link[]> Links { get; }
        public IReadOnlyDictionary<string, Resource[]> Embedded { get; }
    }
}