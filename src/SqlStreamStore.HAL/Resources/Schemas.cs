namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal static class Schemas
    {
        private static readonly ConcurrentDictionary<string, JObject> s_schemas
            = new ConcurrentDictionary<string, JObject>();

        public static JObject AppendToStream => GetSchema(nameof(AppendToStream));
        public static JObject SetStreamMetadata => GetSchema(nameof(SetStreamMetadata));

        private static JObject GetSchema(string name) => s_schemas.GetOrAdd(name, ReadSchema);

        private static JObject ReadSchema(string name)
        {
            using(Stream stream = typeof(Schemas)
                .GetTypeInfo().Assembly
                .GetManifestResourceStream(typeof(Schemas), $"Schema.{name}.json"))
            {
                if(stream == null)
                {
                    throw new Exception($"Embedded resource, {name}, not found. BUG!");
                }

                using(var reader = new JsonTextReader(new StreamReader(stream)))
                {
                    return JObject.Load(reader);
                }
            }
        }
    }
}