namespace SqlStreamStore.HAL.Resources
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using SqlStreamStore.Streams;

    internal class GetStreamMetadataOperation : IStreamStoreOperation<StreamMetadataResult>
    {
        public GetStreamMetadataOperation(IOwinRequest request)
        {
            StreamId = request.Path.Value.Split('/')[1];
        }

        public string StreamId { get; }

        public Task<StreamMetadataResult> Invoke(IStreamStore streamStore, CancellationToken ct)
        {
            return streamStore.GetStreamMetadata(StreamId, ct);
        }
    }
}