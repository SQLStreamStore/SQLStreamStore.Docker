namespace SqlStreamStore.HAL.Resources
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using SqlStreamStore.Streams;

    internal class ReadStreamMessageByStreamVersionOperation : IStreamStoreOperation<StreamMessage>
    {
        public ReadStreamMessageByStreamVersionOperation(HttpRequest request)
        {
            var pieces = request.Path.Value.Split('/').Reverse().Take(2).ToArray();

            StreamId = pieces.LastOrDefault();

            StreamVersion = int.Parse(pieces.First());
        }

        public int StreamVersion { get; }
        public string StreamId { get; }

        public async Task<StreamMessage> Invoke(IStreamStore streamStore, CancellationToken ct)
            => (await streamStore.ReadStreamBackwards(StreamId, StreamVersion, 1, true, ct))
                .Messages.FirstOrDefault(message => StreamVersion == Streams.StreamVersion.End
                                                    || message.StreamVersion == StreamVersion);
    }
}