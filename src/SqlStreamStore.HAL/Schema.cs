namespace SqlStreamStore.HAL
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Reflection;
    using Halcyon.HAL;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class SchemaSet<TResource> where TResource : IResource
    {
        private static readonly ConcurrentDictionary<string, HALResponse> s_schemas
            = new ConcurrentDictionary<string, HALResponse>();

        public HALResponse GetSchema(string name) => s_schemas.GetOrAdd(name, ReadSchema);

        private static HALResponse ReadSchema(string name)
        {
            using(Stream stream = typeof(TResource)
                .GetTypeInfo().Assembly
                .GetManifestResourceStream(typeof(TResource), $"Schema.{name}.json"))
            {
                if(stream == null)
                {
                    throw new Exception($"Embedded resource, {name}, not found. BUG!");
                }

                using(var reader = new JsonTextReader(new StreamReader(stream)))
                {
                    return new HALResponse(JObject.Load(reader));
                }
            }
        }
    }
}