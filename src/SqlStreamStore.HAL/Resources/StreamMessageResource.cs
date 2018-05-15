namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.Streams;

    internal class StreamMessageResource : IResource
    {
        private readonly IStreamStore _streamStore;

        public HttpMethod[] Options { get; } =
        {
            HttpMethod.Get,
            HttpMethod.Head,
            HttpMethod.Options
        };

        public StreamMessageResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
        }
        
        public async Task<Response> GetMessage(
            ReadStreamMessageByStreamVersionOperation operation,
            CancellationToken cancellationToken)
        {
            var message = await operation.Invoke(_streamStore, cancellationToken);

            if(message.MessageId == Guid.Empty)
            {
                return new Response(
                    new HALResponse(new
                        {
                            operation.StreamId,
                            operation.StreamVersion
                        })
                        .AddLinks(Links.StreamMessage.Self(operation))
                        .AddLinks(Links.StreamMessage.Navigation(operation)),
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
                    .AddLinks(Links.StreamMessage.Self(operation))
                    .AddLinks(Links.StreamMessage.Navigation(operation, message)));
        }

        public async Task<Response> DeleteMessage(
            DeleteStreamMessageOperation operation,
            CancellationToken cancellationToken)
        {
            await operation.Invoke(_streamStore, cancellationToken);
            
            return new Response(
                new HALResponse(new HALModelConfig()));
        }
    }
}