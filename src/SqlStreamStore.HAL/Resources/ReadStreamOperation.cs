namespace SqlStreamStore.HAL.Resources
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using SqlStreamStore.Streams;

    internal class ReadStreamOperation : IStreamStoreOperation<ReadStreamPage>
    {
        private readonly int _fromVersionInclusive;
        private readonly int _maxCount;

        public ReadStreamOperation(HttpRequest request)
        {
            StreamId = request.Path.Value.Remove(0, 1);

            EmbedPayload = request.Query.TryGetValue("e", out _);

            ReadDirection = request.Query["d"] == "f"
                ? Constants.ReadDirection.Forwards
                : Constants.ReadDirection.Backwards;

            if(!int.TryParse(request.Query["p"], out _fromVersionInclusive))
            {
                _fromVersionInclusive = ReadDirection == Constants.ReadDirection.Forwards
                    ? StreamVersion.Start
                    : StreamVersion.End;
            }

            if(!int.TryParse(request.Query["m"], out _maxCount))
            {
                _maxCount = Constants.MaxCount;
            }

            Self = ReadDirection == Constants.ReadDirection.Forwards
                ? LinkFormatter.FormatForwardLink(StreamId, MaxCount, FromVersionInclusive, EmbedPayload)
                : LinkFormatter.FormatBackwardLink(StreamId, MaxCount, FromVersionInclusive, EmbedPayload);

            IsUriCanonical = Self.Remove(0, StreamId.Length)
                             == request.QueryString.ToUriComponent();
        }

        public long FromVersionInclusive => _fromVersionInclusive;
        public int MaxCount => _maxCount;
        public bool EmbedPayload { get; }
        public int ReadDirection { get; }
        public string StreamId { get; }
        public string Self { get; }
        public bool IsUriCanonical { get; }

        public Task<ReadStreamPage> Invoke(IStreamStore streamStore, CancellationToken ct)
            => ReadDirection == Constants.ReadDirection.Forwards
                ? streamStore.ReadStreamForwards(StreamId, _fromVersionInclusive, _maxCount, EmbedPayload, ct)
                : streamStore.ReadStreamBackwards(StreamId, _fromVersionInclusive, _maxCount, EmbedPayload, ct);
    }
}