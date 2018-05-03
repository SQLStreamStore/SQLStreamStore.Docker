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
            ReadStreamMessageByStreamVersionOptions options,
            CancellationToken cancellationToken)
        {
            var operation = options.GetReadOperation();

            var message = await operation.Invoke(_streamStore, cancellationToken);

            if(message.MessageId == Guid.Empty)
            {
                return new Response(
                    new HALResponse(new
                        {
                            options.StreamId,
                            options.StreamVersion
                        })
                        .AddLinks(Links.StreamMessage.Self(options))
                        .AddLinks(Links.StreamMessage.Navigation(options))
                        .AddLinks(Links.Stream.Feed(options)),
                    404);
            }
            
            if(options.StreamVersion == StreamVersion.End)
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
                    .AddLinks(Links.StreamMessage.Self(options))
                    .AddLinks(Links.StreamMessage.Navigation(options, message))
                    .AddLinks(Links.Stream.Feed(options)));
        }

        public async Task<Response> DeleteMessage(
            DeleteStreamMessageOptions options,
            CancellationToken cancellationToken)
        {
            var operation = options.GetDeleteOperation();

            await operation.Invoke(_streamStore, cancellationToken);
            
            return new Response(
                new HALResponse(new HALModelConfig()));
        }
    }
}