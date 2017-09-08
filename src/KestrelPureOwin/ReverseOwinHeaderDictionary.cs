using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace KestrelPureOwin
{
    internal class ReverseOwinHeaderDictionary : IHeaderDictionary
    {
        public ReverseOwinHeaderDictionary(IDictionary<string, string[]> inner)
        {
            Inner = inner;
        }

        private IDictionary<string, string[]> Inner { get; }

        public int Count => Inner.Count;

        public bool IsReadOnly => Inner.IsReadOnly;

        public ICollection<string> Keys => Inner.Keys;

        public ICollection<StringValues> Values => Inner.Values.Select(Convert).ToArray();

        StringValues IHeaderDictionary.this[string key]
        {
            get
            {
                string[] values;
                return Inner.TryGetValue(key, out values) ? values : null;
            }
            set { Inner[key] = value; }
        }

        StringValues IDictionary<string, StringValues>.this[string key]
        {
            get { return Inner[key]; }
            set { Inner[key] = value; }
        }

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
        {
            return Inner.Select(Convert).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, StringValues> item)
        {
            Inner.Add(Convert(item));
        }

        public void Clear()
        {
            Inner.Clear();
        }

        public bool Contains(KeyValuePair<string, StringValues> item)
        {
            return Inner.Contains(Convert(item));
        }

        public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            foreach (var pair in Inner)
            {
                array[arrayIndex++] = Convert(pair);
            }
        }

        public bool Remove(KeyValuePair<string, StringValues> item)
        {
            return Inner.Remove(Convert(item));
        }

        public bool ContainsKey(string key)
        {
            return Inner.ContainsKey(key);
        }

        public void Add(string key, StringValues value)
        {
            Inner.Add(key, Convert(value));
        }

        public bool Remove(string key)
        {
            return Inner.Remove(key);
        }

        public bool TryGetValue(string key, out StringValues value)
        {
            string[] values;
            if (Inner.TryGetValue(key, out values))
            {
                value = Convert(values);
                return true;
            }

            value = StringValues.Empty;
            return false;
        }

        private static KeyValuePair<string, StringValues> Convert(KeyValuePair<string, string[]> item)
        {
            return new KeyValuePair<string, StringValues>(item.Key, Convert(item.Value));
        }

        private static KeyValuePair<string, string[]> Convert(KeyValuePair<string, StringValues> item)
        {
            return new KeyValuePair<string, string[]>(item.Key, Convert(item.Value));
        }

        private static StringValues Convert(string[] values)
        {
            return new StringValues(values);
        }

        private static string[] Convert(StringValues items)
        {
            return items.ToArray();
        }
    }
}