namespace SqlStreamStore.HAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.Streams;

    internal class StreamResource
    {
        private readonly IStreamStore _streamStore;

        public StreamResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
        }

        public async Task<Response> AppendMessages(
            AppendStreamOptions options,
            CancellationToken cancellationToken)
        {
            var operation = options.GetAppendOperation();

            var result = await operation.Invoke(_streamStore, cancellationToken);

            return new Response(
                new HALResponse(new object()),
                options.ExpectedVersion == ExpectedVersion.NoStream
                    ? 201
                    : 200);
        }

        public async Task<Response> GetMessage(
            ReadStreamMessageOptions options,
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
                        .AddLinks(StreamMessageLinks.Self(options))
                        .AddLinks(StreamMessageLinks.Navigation(options))
                        .AddLinks(StreamLinks.Feed(options)),
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
                    })
                    .AddLinks(StreamMessageLinks.Self(options))
                    .AddLinks(StreamMessageLinks.Navigation(options, message))
                    .AddLinks(StreamLinks.Feed(options)));
        }

        public async Task<Response> GetPage(ReadStreamOptions options, CancellationToken cancellationToken)
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
                    .AddLinks(StreamLinks.Self(options))
                    .AddLinks(StreamLinks.Navigation(page, options.Self))
                    .AddLinks(StreamLinks.Feed(page))
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
                            }).AddLinks(StreamMessageLinks.Self(message)))));
        }

        private static class StreamMessageLinks
        {
            public static Link Self(ReadStreamMessageOptions options) => new Link(
                Relations.Self,
                $"{options.StreamVersion}");

            public static Link Self(StreamMessage message) => new Link(
                Relations.Self,
                $"{message.StreamId}/{message.StreamVersion}");

            public static Link First(ReadStreamPage page) => new Link(
                Relations.First,
                LinkFormatter.FormatForwardLink(page.StreamId, 20, StreamVersion.Start));

            public static Link Previous(ReadStreamPage page) => new Link(
                Relations.Previous,
                LinkFormatter.FormatBackwardLink(page.StreamId, 20, page.Messages.Min(m => m.StreamVersion) - 1));

            public static Link Next(ReadStreamPage page) => new Link(
                Relations.Next,
                LinkFormatter.FormatForwardLink(page.StreamId, 20, page.Messages.Max(m => m.StreamVersion) + 1));

            public static Link Last(ReadStreamPage page) => new Link(
                Relations.Last,
                LinkFormatter.FormatBackwardLink(page.StreamId, 20, StreamVersion.End));

            private static Link First() => new Link(Relations.First, $"{0}");

            private static Link Previous(ReadStreamMessageOptions options) => new Link(
                Relations.Previous,
                $"{options.StreamVersion - 1}");

            private static Link Next(ReadStreamMessageOptions options) => new Link(
                Relations.Next,
                $"{options.StreamVersion + 1}");

            private static Link Last() => new Link(Relations.Last, $"{-1}");

            public static IEnumerable<Link> Navigation(
                ReadStreamMessageOptions options,
                StreamMessage message = default(StreamMessage))
            {
                yield return First();

                if(options.StreamVersion > 0)
                {
                    yield return Previous(options);
                }

                if(message.MessageId != default(Guid))
                {
                    yield return Next(options);
                }

                yield return Last();
            }
        }

        private static class StreamLinks
        {
            public static Link First(ReadStreamPage page) => new Link(
                Relations.First,
                LinkFormatter.FormatForwardLink(page.StreamId, 20, StreamVersion.Start));

            public static Link Previous(ReadStreamPage page) => new Link(
                Relations.Previous,
                LinkFormatter.FormatBackwardLink(page.StreamId, 20, page.Messages.Min(m => m.StreamVersion) - 1));

            public static Link Next(ReadStreamPage page) => new Link(
                Relations.Next,
                LinkFormatter.FormatForwardLink(page.StreamId, 20, page.Messages.Max(m => m.StreamVersion) + 1));

            public static Link Last(ReadStreamPage page) => new Link(
                Relations.Last,
                LinkFormatter.FormatBackwardLink(page.StreamId, 20, StreamVersion.End));

            public static Link Self(ReadStreamOptions options) => new Link(
                Relations.Self,
                options.Self);

            public static Link Feed(ReadStreamPage page) => new Link(Relations.Feed, Last(page).Href);

            public static Link Feed(ReadStreamMessageOptions options) => new Link(
                Relations.Feed,
                LinkFormatter.FormatBackwardLink(options.StreamId, 20, StreamVersion.End));

            public static IEnumerable<Link> Navigation(ReadStreamPage page, string self)
            {
                var first = First(page);

                var last = Last(page);

                yield return first;

                if(self != first.Href && !page.IsEnd)
                    yield return Previous(page);

                if(self != last.Href && !page.IsEnd)
                    yield return Next(page);

                yield return last;
            }
        }
    }
}