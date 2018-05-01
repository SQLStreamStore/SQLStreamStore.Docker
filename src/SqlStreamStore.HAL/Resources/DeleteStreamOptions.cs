namespace SqlStreamStore.HAL.Resources
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

            ExpectedVersion = request.GetExpectedVersion();
        }

        public string StreamId { get; }
        public int ExpectedVersion { get; }

        public Func<IStreamStore, CancellationToken, Task> GetDeleteOperation()
            => (streamStore, ct) => streamStore.DeleteStream(StreamId, ExpectedVersion, ct);
    }
}
