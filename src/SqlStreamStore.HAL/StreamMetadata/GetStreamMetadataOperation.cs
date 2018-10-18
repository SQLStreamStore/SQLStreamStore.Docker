namespace SqlStreamStore.HAL.StreamMetadata
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using SqlStreamStore.Streams;

    internal class GetStreamMetadataOperation : IStreamStoreOperation<StreamMetadataResult>
    {
        public GetStreamMetadataOperation(HttpRequest request)
        {
            StreamId = request.Path.Value.Split('/')[1];
        }

        public string StreamId { get; }

        public Task<StreamMetadataResult> Invoke(IStreamStore streamStore, CancellationToken ct)
            => streamStore.GetStreamMetadata(StreamId, ct);
    }
}