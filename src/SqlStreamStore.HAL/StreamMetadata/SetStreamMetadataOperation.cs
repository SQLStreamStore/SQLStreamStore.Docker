namespace SqlStreamStore.HAL.StreamMetadata
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class SetStreamMetadataOperation : IStreamStoreOperation<Unit>
    {
        public static async Task<SetStreamMetadataOperation> Create(HttpRequest request, CancellationToken ct)
        {
            using(var reader = new JsonTextReader(new StreamReader(request.Body))
            {
                CloseInput = false
            })
            {
                var body = await JObject.LoadAsync(reader, ct);
                
                return new SetStreamMetadataOperation(request, body);
            }
        }

        private SetStreamMetadataOperation(HttpRequest request, JObject body)
        {
            StreamId = request.Path.Value.Split('/')[1];
            ExpectedVersion = request.GetExpectedVersion();
            MaxAge = body.Value<int?>("maxAge");
            MaxCount = body.Value<int?>("maxCount");
            MetadataJson = Normalize(body["metadataJson"]?.ToString(Formatting.Indented));
        }

        public string StreamId { get; }
        public int ExpectedVersion { get; }
        public string MetadataJson { get; }
        public int? MaxCount { get; }
        public int? MaxAge { get; }

        public async Task<Unit> Invoke(IStreamStore streamStore, CancellationToken ct)
        {
            await streamStore.SetStreamMetadata(
                StreamId,
                ExpectedVersion,
                MaxAge,
                MaxCount,
                MetadataJson,
                ct);

            return Unit.Instance;
        }

        private static string Normalize(string metadataJson)
        {
            if(string.IsNullOrEmpty(metadataJson))
            {
                return metadataJson;
            }

            if(metadataJson[0] == '\'' || metadataJson[0] == '"')
            {
                metadataJson = metadataJson.Remove(0, 1);
            }

            if(metadataJson[metadataJson.Length - 1] == '\'' || metadataJson[metadataJson.Length - 1] == '"')
            {
                metadataJson = metadataJson.Remove(metadataJson.Length - 1, 1);
            }

            return metadataJson;
        }
    }
}