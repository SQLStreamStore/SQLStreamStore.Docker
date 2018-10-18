namespace SqlStreamStore.HAL.StreamMessage
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.Streams;

    internal class StreamMessageResource : IResource
    {
        private readonly IStreamStore _streamStore;
        private readonly SchemaSet<StreamMessageResource> _schema = new SchemaSet<StreamMessageResource>();

        public StreamMessageResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
        }

        private HALResponse DeleteStreamMessage => _schema.GetSchema(nameof(DeleteStreamMessage));

        public async Task<Response> Get(
            ReadStreamMessageByStreamVersionOperation operation,
            CancellationToken cancellationToken)
        {
            var message = await operation.Invoke(_streamStore, cancellationToken);

            var links = Links
                .RootedAt("../../")
                .Index()
                .Find()
                .StreamMessageNavigation(message, operation);
            
            if(message.MessageId == Guid.Empty)
            {
                return new Response(
                    new HALResponse(new
                        {
                            operation.StreamId,
                            operation.StreamVersion
                        })
                        .AddLinks(links),
                    404);
            }

            if(operation.StreamVersion == StreamVersion.End)
            {
                return new Response(new HALResponse(new object()), 307)
                {
                    Headers =
                    {
                        [Constants.Headers.Location] = new[] { $"{message.StreamVersion}" }
                    }
                };
            }

            var payload = await message.GetJsonData(cancellationToken);

            var eTag = ETag.FromStreamVersion(message.StreamVersion);

            return new Response(
                new HALResponse(new
                    {
                        message.MessageId,
                        message.CreatedUtc,
                        message.Position,
                        message.StreamId,
                        message.StreamVersion,
                        message.Type,
                        payload,
                        metadata = message.JsonMetadata
                    })
                    .AddEmbeddedResource(
                        Constants.Relations.Delete,
                        DeleteStreamMessage)
                    .AddLinks(links))
            {
                Headers =
                {
                    eTag,
                    CacheControl.OneYear
                }
            };
        }

        public async Task<Response> Delete(
            DeleteStreamMessageOperation operation,
            CancellationToken cancellationToken)
        {
            await operation.Invoke(_streamStore, cancellationToken);

            return new Response(
                new HALResponse(new HALModelConfig()));
        }
    }
}