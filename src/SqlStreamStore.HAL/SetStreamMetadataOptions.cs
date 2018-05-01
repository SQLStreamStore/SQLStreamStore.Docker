namespace SqlStreamStore.HAL
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class SetStreamMetadataOptions
    {
        public static async Task<SetStreamMetadataOptions> Create(IOwinRequest request, CancellationToken ct)
        {
            using(var reader = new JsonTextReader(new StreamReader(request.Body))
            {
                CloseInput = false
            })
            {
                var body = await JObject.LoadAsync(reader, ct);
                
                return new SetStreamMetadataOptions(request, body);
            }
        }

        private SetStreamMetadataOptions(IOwinRequest request, JObject body)
        {
            StreamId = request.Path.Value.Split('/')[1];
            ExpectedVersion = request.GetExpectedVersion();
            MaxAge = body.Value<int?>("maxAge");
            MaxCount = body.Value<int?>("maxCount");
            MetaJson = body["metaJson"]?.ToString(Formatting.Indented);
        }

        public string StreamId { get; }
        public int ExpectedVersion { get; }
        public string MetaJson { get; }
        public int? MaxCount { get; }
        public int? MaxAge { get; }

        public Func<IStreamStore, CancellationToken, Task> GetSetOperation()
            => (streamStore, ct) => streamStore.SetStreamMetadata(
                StreamId,
                ExpectedVersion,
                MaxAge,
                MaxCount,
                MetaJson,
                ct);
    }
}