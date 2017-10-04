namespace SqlStreamStore.HAL
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using SqlStreamStore.Streams;

    internal class AppendStreamOptions
    {
        public static async Task<AppendStreamOptions> Create(IOwinRequest request, CancellationToken ct)
        {
            using(var reader = new JsonTextReader(new StreamReader(request.Body))
            {
                CloseInput = false
            })
            {
                var body = await JObject.LoadAsync(reader, ct);

                return new AppendStreamOptions(request, body);
            }
        }
        
        private AppendStreamOptions(IOwinRequest request, JObject body)
        {
            StreamId = request.Path.Value.Remove(0, 1);

            ExpectedVersion = body.Value<int>("expectedVersion");

            NewStreamMessages = body.Value<JArray>("messages")
                .Select(newStreamMessage => new NewStreamMessage(
                    Guid.Parse(newStreamMessage.Value<string>("messageId")),
                    newStreamMessage.Value<string>("type"),
                    newStreamMessage.Value<JObject>("jsonData").ToString(),
                    newStreamMessage.Value<JObject>("jsonMetadata")?.ToString()))
                .ToArray();
        }

        public string StreamId { get; }
        public int ExpectedVersion { get; }
        public NewStreamMessage[] NewStreamMessages { get; }

        public Func<IStreamStore, CancellationToken, Task<AppendResult>> GetAppendOperation()
            => (streamStore, ct) => streamStore.AppendToStream(StreamId, ExpectedVersion, NewStreamMessages, ct);
    }
}