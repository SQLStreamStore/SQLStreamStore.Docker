namespace SqlStreamStore.HAL.Resources
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;

    internal class DeleteStreamOperation : IStreamStoreOperation<Unit>
    {
        public DeleteStreamOperation(IOwinRequest request)
        {
            StreamId = request.Path.Value.Remove(0, 1);

            ExpectedVersion = request.GetExpectedVersion();
        }

        public string StreamId { get; }
        public int ExpectedVersion { get; }

        public async Task<Unit> Invoke(IStreamStore streamStore, CancellationToken ct)
        {
            await streamStore.DeleteStream(StreamId, ExpectedVersion, ct);

            return Unit.Instance;
        }
    }
}
