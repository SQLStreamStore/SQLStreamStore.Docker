using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace KestrelPureOwin
{
    internal class OwinHeaderDictionary : IDictionary<string, string[]>
    {
        public OwinHeaderDictionary(IHeaderDictionary inner)
        {
            Inner = inner;
        }

        private IHeaderDictionary Inner { get; }

        public int Count => Inner.Count;

        public bool IsReadOnly => Inner.IsReadOnly;

        public ICollection<string> Keys => Inner.Keys;

        public ICollection<string[]> Values => Inner.Values.Select(Convert).ToList();

        public string[] this[string key]
        {
            get { return Inner[key].ToArray(); }
            set { Inner[key] = value; }
        }

        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            return Inner.Select(Convert).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, string[]> item)
        {
            Inner.Add(Convert(item));
        }

        public void Clear()
        {
            Inner.Clear();
        }

        public bool Contains(KeyValuePair<string, string[]> item)
        {
            return Inner.Contains(Convert(item));
        }

        public void CopyTo(KeyValuePair<string, string[]>[] array, int arrayIndex)
        {
            foreach (var pair in Inner)
            {
                array[arrayIndex++] = Convert(pair);
            }
        }

        public bool Remove(KeyValuePair<string, string[]> item)
        {
            return Inner.Remove(Convert(item));
        }

        public void Add(string key, string[] value)
        {
            Inner.Add(key, Convert(value));
        }

        public bool ContainsKey(string key)
        {
            return Inner.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return Inner.Remove(key);
        }

        public bool TryGetValue(string key, out string[] value)
        {
            StringValues values;
            if (Inner.TryGetValue(key, out values))
            {
                value = Convert(values);
                return true;
            }

            value = null;
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
