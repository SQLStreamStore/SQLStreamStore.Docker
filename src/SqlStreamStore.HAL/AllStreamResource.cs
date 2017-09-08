namespace SqlStreamStore.HAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.Streams;

    internal class AllStreamResource
    {
        private const string StreamId = "stream";

        private readonly IReadonlyStreamStore _streamStore;

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

            return new Response(
                new HALResponse(new object())
                    .AddLinks(Links.SelfFeed(options))
                    .AddLinks(Links.Navigation(page, options.Self))
                    .AddLinks(Links.Feed)
                    .AddEmbeddedCollection(
                        Relations.Message,
                        page.Messages.Zip(payloads,
                            (message, payload) => new HALResponse(new
                            {
                                message.MessageId,
                                message.CreatedUtc,
                                message.Position,
                                message.StreamId,
                                message.StreamVersion,
                                message.Type,
                                payload
                            }).AddLinks(
                                Links.Self(message)))));
        }

        public async Task<Response> GetMessage(
            ReadAllStreamMessageOptions options,
            CancellationToken cancellationToken)
        {
            var operation = options.GetReadOperation();

            var message = await operation.Invoke(_streamStore, cancellationToken);

            if(message.MessageId == Guid.Empty)
            {
                return new Response(
                    new HALResponse(new HALModelConfig())
                        .AddLinks(Links.Feed),
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
                    payload
                }).AddLinks(
                    Links.SelfAll(message),
                    Links.Feed));
        }

        private static class Links
        {
            public static Link SelfFeed(ReadAllStreamOptions options)
                => new Link(Relations.Self, options.Self);

            public static Link Self(StreamMessage message) => new Link(
                Relations.Self,
                $"streams/{message.StreamId}/{message.StreamVersion}");

            public static Link SelfAll(StreamMessage message) => new Link(Relations.Self, $"/{StreamId}/{message.Position}");

            public static Link First => new Link(Relations.First,
                LinkFormatter.FormatForwardLink(StreamId, 20, Position.Start));

            public static Link Last => new Link(Relations.Last, LinkFormatter.FormatBackwardLink(StreamId, 20, Position.End));

            public static Link Feed => new Link(Relations.Feed, Last.Href);

            public static Link Previous(ReadAllPage page)
                => new Link(Relations.Previous,
                    LinkFormatter.FormatBackwardLink(StreamId, 20, page.Messages.Min(m => m.Position) - 1));

            public static Link Next(ReadAllPage page)
                => new Link(Relations.Next,
                    LinkFormatter.FormatForwardLink(StreamId, 20, page.Messages.Max(m => m.Position) + 1));

            public static IEnumerable<Link> Navigation(ReadAllPage page, string self)
            {
                yield return First;

                if(self != First.Href && !page.IsEnd)
                    yield return Previous(page);

                if(self != Last.Href && !page.IsEnd)
                    yield return Next(page);

                yield return Last;
            }
        }
    }
}