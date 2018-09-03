namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.Streams;

    internal class AllStreamResource : IResource
    {
        private readonly IStreamStore _streamStore;

        public HttpMethod[] Allowed { get; } =
        {
            HttpMethod.Get,
            HttpMethod.Head,
            HttpMethod.Options
        };

        public AllStreamResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
        }

        public async Task<Response> Get(
            ReadAllStreamOperation operation,
            CancellationToken cancellationToken)
        {
            if(!operation.IsUriCanonical)
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
                    .AddLinks(Links.Self(operation))
                    .AddLinks(Links.Navigation(page, operation))
                    .AddLinks(Links.Feed(operation))
                    .AddLinks(Links.Find())
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

            return response;
        }

        private static class Links
        {
            public static Link Find() => SqlStreamStore.HAL.Links.Find("../streams/{streamId}");

            public static Link Self(ReadAllStreamOperation operation)
                => new Link(Constants.Relations.Self, operation.Self);

            public static Link First(ReadAllStreamOperation operation)
                => new Link(
                    Constants.Relations.First,
                    LinkFormatter.FormatForwardLink(
                        Constants.Streams.All,
                        operation.MaxCount,
                        Position.Start,
                        operation.EmbedPayload));

            public static Link Last(ReadAllStreamOperation operation)
                => new Link(
                    Constants.Relations.Last,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        operation.MaxCount,
                        Position.End,
                        operation.EmbedPayload));

            public static Link Last()
                => new Link(
                    Constants.Relations.Last,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        Constants.MaxCount,
                        Position.End,
                        false));

            public static Link Feed(ReadAllStreamOperation operation)
                => new Link(Constants.Relations.Feed, operation.Self);

            public static Link Feed()
                => new Link(Constants.Relations.Feed, Last().Href);

            public static Link Previous(ReadAllPage page, ReadAllStreamOperation operation)
                => new Link(
                    Constants.Relations.Previous,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        operation.MaxCount,
                        page.Messages.Min(m => m.Position) - 1,
                        operation.EmbedPayload));

            public static Link Next(ReadAllPage page, ReadAllStreamOperation operation)
                => new Link(
                    Constants.Relations.Next,
                    LinkFormatter.FormatForwardLink(
                        Constants.Streams.All,
                        operation.MaxCount,
                        page.Messages.Max(m => m.Position) + 1,
                        operation.EmbedPayload));

            public static IEnumerable<Link> Navigation(ReadAllPage page, ReadAllStreamOperation operation)
            {
                var first = First(operation);
                var last = Last(operation);

                yield return first;

                if(operation.Self != first.Href && !page.IsEnd)
                    yield return Previous(page, operation);

                if(operation.Self != last.Href && !page.IsEnd)
                    yield return Next(page, operation);

                yield return last;
            }

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