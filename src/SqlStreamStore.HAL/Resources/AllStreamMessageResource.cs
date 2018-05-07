namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;

    internal class AllStreamMessageResource : IResource
    {
        private readonly IStreamStore _streamStore;

        public HttpMethod[] Options { get; } =
        {
            HttpMethod.Get,
            HttpMethod.Head,
            HttpMethod.Options
        };

        public AllStreamMessageResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
        }
        
        public async Task<Response> GetMessage(
            ReadAllStreamMessageOperation operation,
            CancellationToken cancellationToken)
        {
            var message = await operation.Invoke(_streamStore, cancellationToken);

            if(message.MessageId == Guid.Empty)
            {
                return new Response(
                    new HALResponse(new HALModelConfig())
                        .AddLinks(Links.All.Feed(operation)),
                    404);
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
                }).AddLinks(
                    Links.All.SelfAll(message),
                    Links.All.Feed(operation)));
        }
    }
}