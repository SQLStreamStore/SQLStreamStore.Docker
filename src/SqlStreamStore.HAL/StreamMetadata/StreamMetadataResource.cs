namespace SqlStreamStore.HAL.StreamMetadata
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;

    internal class StreamMetadataResource : IResource
    {
        private readonly IStreamStore _streamStore;
        private readonly SchemaSet<StreamMetadataResource> _schema;

        public StreamMetadataResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
            _schema = new SchemaSet<StreamMetadataResource>();
        }

        private HALResponse SetStreamMetadata => _schema.GetSchema(nameof(SetStreamMetadata));

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
                        SetStreamMetadata),
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