namespace SqlStreamStore.HAL
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using SqlStreamStore.Streams;

    internal class GetStreamMetadataOptions
    {
        public GetStreamMetadataOptions(IOwinRequest request)
        {
            StreamId = request.Path.Value.Split('/')[1];
        }

        public string StreamId { get; }

        public Func<IReadonlyStreamStore, CancellationToken, Task<StreamMetadataResult>> GetReadOperation()
            => (streamStore, ct) => streamStore.GetStreamMetadata(StreamId, ct);
    }
}