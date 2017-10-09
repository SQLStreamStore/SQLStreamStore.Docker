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

            var response = new Response(
                new HALResponse(new object())
                    .AddLinks(Links.SelfFeed(options))
                    .AddLinks(Links.Navigation(page, options))
                    .AddLinks(Links.Feed(options))
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
                                payload
                            }).AddLinks(
                                Links.Self(message)))));

            if(options.FromPositionInclusive == Position.End)
            {
                var headPosition = page.Messages.Length > 0
                    ? page.Messages[0].Position
                    : Position.End;
                
                response.Headers[Constants.Headers.HeadPosition] = new[] { $"{headPosition}" };
            }
            
            return response;
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
                        .AddLinks(Links.Feed(options)),
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
                    Links.Feed(options)));
        }

        private static class Links
        {
            public static Link SelfFeed(ReadAllStreamOptions options)
                => new Link(Constants.Relations.Self, options.Self);

            public static Link Self(StreamMessage message) => new Link(
                Constants.Relations.Self,
                $"streams/{message.StreamId}/{message.StreamVersion}");

            public static Link SelfAll(StreamMessage message) 
                => new Link(Constants.Relations.Self, $"/{Constants.Streams.All}/{message.Position}");

            public static Link First(ReadAllStreamOptions options)
                => new Link(
                    Constants.Relations.First,
                    LinkFormatter.FormatForwardLink(
                        Constants.Streams.All,
                        options.MaxCount,
                        Position.Start,
                        options.EmbedPayload));

            public static Link Last(ReadAllStreamOptions options)
                => new Link(
                    Constants.Relations.Last,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        options.MaxCount,
                        Position.End,
                        options.EmbedPayload));

            public static Link Last(ReadAllStreamMessageOptions options)
                => new Link(
                    Constants.Relations.Last,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        Constants.MaxCount,
                        Position.End,
                        false));

            public static Link Feed(ReadAllStreamOptions options) 
                => new Link(Constants.Relations.Feed, Last(options).Href);

            public static Link Feed(ReadAllStreamMessageOptions options) 
                => new Link(Constants.Relations.Feed, Last(options).Href);

            public static Link Previous(ReadAllPage page, ReadAllStreamOptions options)
                => new Link(
                    Constants.Relations.Previous,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        options.MaxCount,
                        page.Messages.Min(m => m.Position) - 1,
                        options.EmbedPayload));

            public static Link Next(ReadAllPage page, ReadAllStreamOptions options)
                => new Link(
                    Constants.Relations.Next,
                    LinkFormatter.FormatForwardLink(
                        Constants.Streams.All,
                        options.MaxCount,
                        page.Messages.Max(m => m.Position) + 1,
                        options.EmbedPayload));

            public static IEnumerable<Link> Navigation(ReadAllPage page, ReadAllStreamOptions options)
            {
                var first = First(options);
                var last = Last(options);
                
                yield return first;

                if(options.Self != first.Href && !page.IsEnd)
                    yield return Previous(page, options);

                if(options.Self != last.Href && !page.IsEnd)
                    yield return Next(page, options);

                yield return last;
            }
        }
    }
}