namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;

    internal class StreamMetadataResource : IResource
    {
        private static readonly Link[] s_links =
        {
            new Link(Constants.Relations.Self, Constants.Streams.Metadata),
            new Link(Constants.Relations.Feed, "../")
        };

        private readonly IStreamStore _streamStore;

        public HttpMethod[] Options { get; } =
        {
            HttpMethod.Get,
            HttpMethod.Head,
            HttpMethod.Options,
            HttpMethod.Post
        };

        public StreamMetadataResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
        }

        public async Task<Response> GetStreamMetadata(
            GetStreamMetadataOptions options,
            CancellationToken cancellationToken)
        {
            var operation = options.GetReadOperation();

            var result = await operation.Invoke(_streamStore, cancellationToken);

            var response = new Response(new HALResponse(new
                {
                    result.StreamId,
                    result.MetadataStreamVersion,
                    result.MaxAge,
                    result.MaxCount,
                    MetaJson = result.MetadataJson
                }).AddLinks(s_links),
                result.MetadataStreamVersion >= 0 ? 200 : 404);

            return response;
        }

        public async Task<Response> SetStreamMetadata(
            SetStreamMetadataOptions options,
            CancellationToken cancellationToken)
        {
            var operation = options.GetSetOperation();

            await operation.Invoke(_streamStore, cancellationToken);

            var response = new Response(new HALResponse(new
                {
                    options.StreamId,
                    options.MaxAge,
                    options.MaxCount,
                    options.MetaJson
                })
                .AddLinks(s_links));

            return response;
        }
    }
}