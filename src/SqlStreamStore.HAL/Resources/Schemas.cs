namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Reflection;
    using Halcyon.HAL;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal static class Schemas
    {
        private static readonly ConcurrentDictionary<string, HALResponse> s_schemas
            = new ConcurrentDictionary<string, HALResponse>();

        public static HALResponse AppendToStream => GetSchema(nameof(AppendToStream));
        public static HALResponse SetStreamMetadata => GetSchema(nameof(SetStreamMetadata));
        public static HALResponse DeleteStream => GetSchema(nameof(DeleteStream));
        public static HALResponse DeleteStreamMessage => GetSchema(nameof(DeleteStreamMessage));

        private static HALResponse GetSchema(string name) => s_schemas.GetOrAdd(name, ReadSchema);

        private static HALResponse ReadSchema(string name)
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
                    return new HALResponse(JObject.Load(reader));
                }
            }
        }
    }
}