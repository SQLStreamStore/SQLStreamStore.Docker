namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;

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
                        Links.Self(),
                        Links.Metadata(),
                        Links.Feed(operation))
                    .AddEmbeddedResource(
                        Constants.Relations.Metadata,
                        Schemas.SetStreamMetadata),
                result.MetadataStreamVersion >= 0 ? 200 : 404);

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
                    Links.Self(),
                    Links.Metadata(),
                    Links.Feed(operation)));

            return response;
        }

        private static class Links
        {
            public static Link Find() => SqlStreamStore.HAL.Links.Find("../{streamId}");
            public static Link Self() => new Link(Constants.Relations.Self, Constants.Streams.Metadata);
            public static Link Metadata() => new Link(Constants.Relations.Metadata, Constants.Streams.Metadata);
            public static Link Feed(GetStreamMetadataOperation operation) => Link(operation.StreamId);
            public static Link Feed(SetStreamMetadataOperation operation) => Link(operation.StreamId);

            private static Link Link(string streamId)
                => new Link(Constants.Relations.Feed, $"../{streamId}");
        }
    }
}