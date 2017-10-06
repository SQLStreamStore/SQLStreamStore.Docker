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
                var body = await JToken.LoadAsync(reader, ct);

                switch(body)
                {
                    case JArray json:
                        return new AppendStreamOptions(request, json);
                    case JObject json:
                        return new AppendStreamOptions(request, json);
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private AppendStreamOptions(IOwinRequest request)
        {
            StreamId = request.Path.Value.Remove(0, 1);

            ExpectedVersion = int.TryParse(
                request.Headers.Get(Constants.Headers.ExpectedVersion),
                out var expectedVersion)
                ? expectedVersion
                : Streams.ExpectedVersion.Any;
        }

        private AppendStreamOptions(IOwinRequest request, JArray body)
            : this(request)
        {
            NewStreamMessages = body.Select(newStreamMessage => new NewStreamMessage(
                    Guid.Parse(newStreamMessage.Value<string>("messageId")),
                    newStreamMessage.Value<string>("type"),
                    newStreamMessage.Value<JObject>("jsonData").ToString(),
                    newStreamMessage.Value<JObject>("jsonMetadata")?.ToString()))
                .ToArray();
        }

        private AppendStreamOptions(IOwinRequest request, JObject body)
            : this(request)
        {
            NewStreamMessages = new[]
            {
                new NewStreamMessage(
                    Guid.Parse(body.Value<string>("messageId")),
                    body.Value<string>("type"),
                    body.Value<JObject>("jsonData").ToString(),
                    body.Value<JObject>("jsonMetadata")?.ToString())
            }; 
        }

        public string StreamId { get; }
        public int ExpectedVersion { get; }
        public NewStreamMessage[] NewStreamMessages { get; }

        public Func<IStreamStore, CancellationToken, Task<AppendResult>> GetAppendOperation()
            => (streamStore, ct) => streamStore.AppendToStream(StreamId, ExpectedVersion, NewStreamMessages, ct);
    }
}