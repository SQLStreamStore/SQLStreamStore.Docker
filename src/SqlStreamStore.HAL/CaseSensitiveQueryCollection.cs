namespace SqlStreamStore.HAL
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.Extensions.Primitives;

    internal class CaseSensitiveQueryFeature : IQueryFeature
    {
        private readonly IFeatureCollection _features;

        public CaseSensitiveQueryFeature(IFeatureCollection features)
        {
            if(features == null)
                throw new ArgumentNullException(nameof(features));
            _features = features;
        }

        public IQueryCollection Query
        {
            get;
            set;
        }
    }

    internal class CaseSensitiveQueryCollection : IQueryCollection
    {
        private readonly QueryString _queryString;

        private Dictionary<string, StringValues> _state;

        public CaseSensitiveQueryCollection(QueryString queryString)
        {
            _queryString = queryString;
        }

        private Dictionary<string, StringValues> GetState()
            => _state
               ?? (_state = QueryStringHelper.ParseQueryString(_queryString));

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
            => GetState().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public bool ContainsKey(string key)
            => GetState().ContainsKey(key);

        public bool TryGetValue(string key, out StringValues value)
            => GetState().TryGetValue(key, out value);

        public int Count
            => GetState().Count;

        public ICollection<string> Keys
            => GetState().Keys;

        public StringValues this[string key]
            => GetState()[key];
    }
}