namespace SqlStreamStore.HAL
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;

    internal class DeleteStreamOptions
    {
        public DeleteStreamOptions(IOwinRequest request)
        {
            StreamId = request.Path.Value.Remove(0, 1);

            ExpectedVersion = int.TryParse(
                request.Headers.Get(Constants.Headers.ExpectedVersion),
                out var expectedVersion)
                ? expectedVersion
                : Streams.ExpectedVersion.Any;
        }

        public string StreamId { get; }
        public int ExpectedVersion { get; }

        public Func<IStreamStore, CancellationToken, Task> GetDeleteOperation()
            => (streamStore, ct) => streamStore.DeleteStream(StreamId, ExpectedVersion, ct);
    }
}