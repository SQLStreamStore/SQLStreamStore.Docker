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
        public SchemaSet Schema { get; }

        public StreamMessageResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
            Schema = new SchemaSet<StreamMessageResource>();
        }

        private HALResponse DeleteStreamMessage => Schema.GetSchema("delete-message");

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
                return new HalJsonResponse(
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
                return new HalJsonResponse(new HALResponse(new object()), 307)
                {
                    Headers =
                    {
                        [Constants.Headers.Location] = new[] { $"{message.StreamVersion}" }
                    }
                };
            }

            var payload = await message.GetJsonData(cancellationToken);

            var eTag = ETag.FromStreamVersion(message.StreamVersion);

            return new HalJsonResponse(
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
                        Constants.Relations.DeleteMessage,
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

            return new HalJsonResponse(
                new HALResponse(new HALModelConfig()));
        }
    }
}