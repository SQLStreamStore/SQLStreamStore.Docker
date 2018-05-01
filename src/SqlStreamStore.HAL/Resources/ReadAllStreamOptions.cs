namespace SqlStreamStore.HAL.Resources
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
                ? Constants.ReadDirection.Forwards
                : Constants.ReadDirection.Backwards;

            if(!long.TryParse(request.Query.Get("p"), out _fromPositionInclusive))
            {
                _fromPositionInclusive = ReadDirection > 0 ? Position.Start : Position.End;
            }

            if(!int.TryParse(request.Query.Get("m"), out _maxCount))
            {
                _maxCount = Constants.MaxCount;
            }
        }

        public long FromPositionInclusive => _fromPositionInclusive;
        public int MaxCount => _maxCount;
        public bool EmbedPayload { get; }
        public int ReadDirection { get; }

        public string Self => ReadDirection == Constants.ReadDirection.Forwards
            ? LinkFormatter.FormatForwardLink(Constants.Streams.All, MaxCount, FromPositionInclusive, EmbedPayload)
            : LinkFormatter.FormatBackwardLink(Constants.Streams.All, MaxCount, FromPositionInclusive, EmbedPayload);

        public Func<IReadonlyStreamStore, CancellationToken, Task<ReadAllPage>> GetReadOperation()
            => (streamStore, ct) => ReadDirection == Constants.ReadDirection.Forwards
                ? streamStore.ReadAllForwards(_fromPositionInclusive, _maxCount, EmbedPayload, ct)
                : streamStore.ReadAllBackwards(_fromPositionInclusive, _maxCount, EmbedPayload, ct);
    }
}