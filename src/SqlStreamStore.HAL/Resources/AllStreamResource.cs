namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.Streams;

    internal class AllStreamResource : IResource
    {
        private readonly IReadonlyStreamStore _streamStore;

        public HttpMethod[] Options { get; } =
        {
            HttpMethod.Get,
            HttpMethod.Head,
            HttpMethod.Options
        };

        public AllStreamResource(IReadonlyStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
        }

        public async Task<Response> GetPage(
            ReadAllStreamOptions options,
            CancellationToken cancellationToken)
        {
            var operation = options.GetReadOperation();

            var page = await operation.Invoke(_streamStore, cancellationToken);

            var payloads = await Task.WhenAll(page.Messages
                .Select(message => options.EmbedPayload
                    ? message.GetJsonData(cancellationToken)
                    : Task.FromResult<string>(null))
                .ToArray());

            var response = new Response(
                new HALResponse(new
                    {
                        page.FromPosition,
                        page.NextPosition,
                        page.IsEnd
                    })
                    .AddLinks(Links.All.SelfFeed(options))
                    .AddLinks(Links.All.Navigation(page, options))
                    .AddLinks(Links.All.Feed(options))
                    .AddEmbeddedCollection(
                        Constants.Relations.Message,
                        page.Messages.Zip(payloads,
                            (message, payload) => new HALResponse(new
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
                                Links.All.Self(message)))));

            if(options.FromPositionInclusive == Position.End)
            {
                var headPosition = page.Messages.Length > 0
                    ? page.Messages[0].Position
                    : Position.End;
                
                response.Headers[Constants.Headers.HeadPosition] = new[] { $"{headPosition}" };
            }
            
            return response;
        }
    }
}