namespace SqlStreamStore.HAL
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using SqlStreamStore.Streams;

    internal class ReadAllStreamOptions
    {
        private readonly long _fromPositionInclusive;
        private readonly int _maxCount;

        public ReadAllStreamOptions(IOwinRequest request)
        {
            EmbedPayload = request.Query.Get("e") != null;

            ReadDirection = request.Query.Get("d") == "f"
                ? 1
                : -1;

            if(!long.TryParse(request.Query.Get("p"), out _fromPositionInclusive))
            {
                _fromPositionInclusive = ReadDirection > 0 ? Position.Start : Position.End;
            }

            if(!int.TryParse(request.Query.Get("m"), out _maxCount))
            {
                _maxCount = 20;
            }
        }

        public long FromPositionInclusive => _fromPositionInclusive;
        public int MaxCount => _maxCount;
        public bool EmbedPayload { get; }
        public int ReadDirection { get; }

        public string Self => ReadDirection > 0
            ? LinkFormatter.FormatForwardLink("stream", MaxCount, FromPositionInclusive)
            : LinkFormatter.FormatBackwardLink("stream", MaxCount, FromPositionInclusive);

        public Func<IReadonlyStreamStore, CancellationToken, Task<ReadAllPage>> GetReadOperation()
            => (streamStore, ct) => ReadDirection > 0
                ? streamStore.ReadAllForwards(_fromPositionInclusive, _maxCount, EmbedPayload, ct)
                : streamStore.ReadAllBackwards(_fromPositionInclusive, _maxCount, EmbedPayload, ct);
    }
}