namespace SqlStreamStore.HAL.AllStream
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
        private readonly IStreamStore _streamStore;
        private readonly bool _useCanonicalUrls;

        public HttpMethod[] Allowed { get; } =
        {
            HttpMethod.Get,
            HttpMethod.Head,
            HttpMethod.Options
        };

        public AllStreamResource(IStreamStore streamStore, bool useCanonicalUrls)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
            _useCanonicalUrls = useCanonicalUrls;
        }

        public async Task<Response> Get(
            ReadAllStreamOperation operation,
            CancellationToken cancellationToken)
        {
            if(_useCanonicalUrls && !operation.IsUriCanonical)
            {
                return new Response(new HALResponse(null), 308)
                {
                    Headers = { [Constants.Headers.Location] = new[] { operation.Self } }
                };
            }

            var page = await operation.Invoke(_streamStore, cancellationToken);

            var streamMessages = page.Messages.OrderByDescending(m => m.Position).ToArray();

            var payloads = await Task.WhenAll(
                Array.ConvertAll(
                    streamMessages,
                    message => operation.EmbedPayload
                        ? message.GetJsonData(cancellationToken)
                        : SkippedPayload.Instance));

            var response = new Response(
                new HALResponse(new
                    {
                        page.FromPosition,
                        page.NextPosition,
                        page.IsEnd
                    })
                    .AddLinks(
                        TheLinks
                            .RootedAt(string.Empty)
                            .Index()
                            .Find()
                            .Navigation(page, operation))
                    .AddEmbeddedCollection(
                        Constants.Relations.Message,
                        streamMessages.Zip(
                            payloads,
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
                                })
                                .AddLinks(
                                    Links.Message.Self(message),
                                    Links.Message.Feed(message)))));

            if(operation.FromPositionInclusive == Position.End)
            {
                var headPosition = streamMessages.Length > 0
                    ? streamMessages[0].Position
                    : Position.End;

                response.Headers[Constants.Headers.HeadPosition] = new[] { $"{headPosition}" };
            }

            if(page.TryGetETag(operation.FromPositionInclusive, out var eTag))
            {
                response.Headers.Add(eTag);
                response.Headers.Add(CacheControl.NoCache);
            }
            else
            {
                response.Headers.Add(CacheControl.OneYear);
            }

            return response;
        }

        private static class Links
        {
            public static class Message
            {
                public static Link Self(StreamMessage message) => new Link(
                    Constants.Relations.Self,
                    $"streams/{message.StreamId}/{message.StreamVersion}");

                public static Link Feed(StreamMessage message) => new Link(
                    Constants.Relations.Feed,
                    $"streams/{message.StreamId}");
            }
        }
    }
}