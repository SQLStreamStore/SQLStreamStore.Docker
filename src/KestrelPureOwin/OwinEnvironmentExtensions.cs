using System.Collections.Generic;

namespace KestrelPureOwin
{
    internal static class OwinEnvironmentExtensions
    {
        public static T Get<T>(this IDictionary<string, object> environment, string key)
        {
            object value;
            if (environment.TryGetValue(key, out value) && value is T)
            {
                return (T) value;
            }

            return default(T);
        }

        public static void Set<T>(this IDictionary<string, object> environment, string key, T value)
        {
            environment[key] = value;
        }
    }
}
