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
            NewStreamMessages = body.Select(ParseNewStreamMessage)
                .ToArray();
        }

        private AppendStreamOptions(IOwinRequest request, JObject body)
            : this(request, new JArray { body })
        { }

        private static NewStreamMessage ParseNewStreamMessage(JToken newStreamMessage, int index)
        {
            if(!Guid.TryParse(newStreamMessage.Value<string>("messageId"), out var messageId))
            {
                throw new InvalidAppendRequestException($"'{nameof(messageId)}' at index {index} was improperly formatted.");
            };
            if(messageId == Guid.Empty)
            {
                throw new InvalidAppendRequestException($"'{nameof(messageId)}' at index {index} was empty.");
            }
            var type = newStreamMessage.Value<string>("type");

            if(type == null)
            {
                throw new InvalidAppendRequestException($"'{nameof(type)}' at index {index} was not set.");
            }
            
            return new NewStreamMessage(
                messageId,
                type,
                newStreamMessage.Value<JToken>("jsonData").ToString(),
                newStreamMessage.Value<JToken>("jsonMetadata")?.ToString());
        }
        public string StreamId { get; }
        public int ExpectedVersion { get; }
        public NewStreamMessage[] NewStreamMessages { get; }

        public Func<IStreamStore, CancellationToken, Task<AppendResult>> GetAppendOperation()
            => (streamStore, ct) => streamStore.AppendToStream(StreamId, ExpectedVersion, NewStreamMessages, ct);
    }
}