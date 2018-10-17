namespace SqlStreamStore.HAL.StreamMetadata
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.HAL.Resources;

    internal class StreamMetadataResource : IResource
    {
        private readonly IStreamStore _streamStore;

        public HttpMethod[] Allowed { get; } =
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

        public async Task<Response> Get(
            GetStreamMetadataOperation operation,
            CancellationToken cancellationToken)
        {
            var result = await operation.Invoke(_streamStore, cancellationToken);

            var response = new Response(new HALResponse(new
                    {
                        result.StreamId,
                        result.MetadataStreamVersion,
                        result.MaxAge,
                        result.MaxCount,
                        result.MetadataJson
                    })
                    .AddLinks(
                        TheLinks
                            .RootedAt("../../../")
                            .Index()
                            .Find()
                            .MetadataNavigation(operation))
                    .AddEmbeddedResource(
                        Constants.Relations.Metadata,
                        Schemas.SetStreamMetadata),
                result.MetadataStreamVersion >= 0 ? 200 : 404)
            {
                Headers =
                {
                    ETag.FromStreamVersion(result.MetadataStreamVersion)
                }
            };

            return response;
        }

        public async Task<Response> Post(
            SetStreamMetadataOperation operation,
            CancellationToken cancellationToken)
        {
            await operation.Invoke(_streamStore, cancellationToken);

            var response = new Response(new HALResponse(new
                {
                    operation.StreamId,
                    operation.MaxAge,
                    operation.MaxCount,
                    operation.MetadataJson
                })
                .AddLinks(
                    TheLinks
                        .RootedAt("../../../")
                        .Index()
                        .Find()
                        .MetadataNavigation(operation)));

            return response;
        }
    }
}