namespace SqlStreamStore.HAL.Streams
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
            Path = request.Path;

            StreamId = request.Path.Value.Remove(0, 2 + Constants.Streams.Stream.Length);

            EmbedPayload = request.Query.TryGetValueCaseInsensitive('e', out var embedPayload)
                           && embedPayload == "1";

            ReadDirection = request.Query.TryGetValueCaseInsensitive('d', out var readDirection)
                            && readDirection == "f" || readDirection == "F"
                ? Constants.ReadDirection.Forwards
                : Constants.ReadDirection.Backwards;

            _fromVersionInclusive = request.Query.TryGetValueCaseInsensitive('p', out var position)
                ? int.TryParse(position, out _fromVersionInclusive)
                    ? ReadDirection == Constants.ReadDirection.Forwards
                        ? _fromVersionInclusive < StreamVersion.Start
                            ? StreamVersion.Start
                            : _fromVersionInclusive
                        : _fromVersionInclusive < StreamVersion.End
                            ? StreamVersion.End
                            : _fromVersionInclusive
                    : ReadDirection == Constants.ReadDirection.Forwards
                        ? StreamVersion.Start
                        : StreamVersion.End
                : ReadDirection == Constants.ReadDirection.Forwards
                    ? StreamVersion.Start
                    : StreamVersion.End;

            _maxCount = request.Query.TryGetValueCaseInsensitive('m', out var maxCount)
                ? int.TryParse(maxCount, out _maxCount)
                    ? _maxCount <= 0
                        ? Constants.MaxCount
                        : _maxCount
                    : Constants.MaxCount
                : Constants.MaxCount;

            var baseAddress = $"streams/{StreamId}";

            Self = ReadDirection == Constants.ReadDirection.Forwards
                ? Links.FormatForwardLink(baseAddress, MaxCount, FromVersionInclusive, EmbedPayload)
                : Links.FormatBackwardLink(baseAddress, MaxCount, FromVersionInclusive, EmbedPayload);

            IsUriCanonical = Self.Remove(0, baseAddress.Length)
                             == request.QueryString.ToUriComponent();
        }

        public PathString Path { get; }
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