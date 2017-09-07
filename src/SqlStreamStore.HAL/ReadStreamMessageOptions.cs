namespace SqlStreamStore.HAL
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using SqlStreamStore.Streams;

    internal class ReadStreamMessageOptions
    {
        public ReadStreamMessageOptions(IOwinRequest request)
        {
            var pieces = request.Path.Value.Split('/').Reverse().Take(2).ToArray();

            StreamId = pieces.LastOrDefault();

            if(int.TryParse(pieces.FirstOrDefault(), out var streamVersion))
            {
                StreamVersion = streamVersion;
            }
        }

        public int StreamVersion { get; }
        public string StreamId { get; }

        public Func<IReadonlyStreamStore, CancellationToken, Task<StreamMessage>> GetReadOperation()
            => async (streamStore, ct) => (await streamStore.ReadStreamBackwards(StreamId, StreamVersion, 1, true, ct))
                .Messages.FirstOrDefault(message => StreamVersion == Streams.StreamVersion.End
                                                    || message.StreamVersion == StreamVersion);
    }
}