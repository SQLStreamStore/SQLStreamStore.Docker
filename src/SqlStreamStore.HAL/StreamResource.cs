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

            var response = new Response(
                new HALResponse(result)
                    .AddLinks(StreamLinks.Self(options))
                    .AddLinks(StreamLinks.Feed(options)),
                result.CurrentVersion == 0
                    ? 201
                    : 200);
            if(options.ExpectedVersion == ExpectedVersion.NoStream)
            {
                response.Headers[Constants.Headers.Location] = new[] { $"streams/{options.StreamId}" };
            }
            return response;
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
                new HALResponse(new
                    {
                        page.LastStreamVersion,
                        page.LastStreamPosition
                    })
                    .AddLinks(StreamLinks.Self(options))
                    .AddLinks(StreamLinks.Navigation(page, options))
                    .AddLinks(StreamLinks.Feed(page, options))
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
                            }).AddLinks(StreamMessageLinks.Self(message)))),
                page.Status == PageReadStatus.StreamNotFound ? 404 : 200);
        }

        public async Task<Response> Delete(DeleteStreamOptions options, CancellationToken cancellationToken)
        {
            var operation = options.GetDeleteOperation();

            await operation.Invoke(_streamStore, cancellationToken);
            
            return new Response(new HALResponse(new object()));
        }

        private static class StreamMessageLinks
        {
            public static Link Self(ReadStreamMessageOptions options) => new Link(
                Constants.Relations.Self,
                $"{options.StreamVersion}");

            public static Link Self(StreamMessage message) => new Link(
                Constants.Relations.Self,
                $"{message.StreamId}/{message.StreamVersion}");

            public static Link First(ReadStreamPage page, ReadStreamOptions options)
                => new Link(
                    Constants.Relations.First,
                    LinkFormatter.FormatForwardLink(
                        page.StreamId,
                        options.MaxCount,
                        StreamVersion.Start,
                        options.EmbedPayload));

            public static Link Previous(ReadStreamPage page, ReadStreamOptions options)
                => new Link(
                    Constants.Relations.Previous,
                    LinkFormatter.FormatBackwardLink(
                        page.StreamId,
                        options.MaxCount,
                        page.Messages.Min(m => m.StreamVersion) - 1,
                        options.EmbedPayload));

            public static Link Next(ReadStreamPage page, ReadStreamOptions options)
                => new Link(
                    Constants.Relations.Next,
                    LinkFormatter.FormatForwardLink(
                        page.StreamId,
                        options.MaxCount,
                        page.Messages.Max(m => m.StreamVersion) + 1,
                        options.EmbedPayload));

            public static Link Last(ReadStreamPage page, ReadStreamOptions options)
                => new Link(
                    Constants.Relations.Last,
                    LinkFormatter.FormatBackwardLink(
                        page.StreamId,
                        options.MaxCount,
                        StreamVersion.End,
                        options.EmbedPayload));

            private static Link First() => new Link(Constants.Relations.First, $"{0}");

            private static Link Previous(ReadStreamMessageOptions options) => new Link(
                Constants.Relations.Previous,
                $"{options.StreamVersion - 1}");

            private static Link Next(ReadStreamMessageOptions options) => new Link(
                Constants.Relations.Next,
                $"{options.StreamVersion + 1}");

            private static Link Last() => new Link(Constants.Relations.Last, $"{-1}");

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
            public static Link First(ReadStreamPage page, ReadStreamOptions options)
                => new Link(
                    Constants.Relations.First,
                    LinkFormatter.FormatForwardLink(
                        page.StreamId,
                        options.MaxCount,
                        StreamVersion.Start,
                        options.EmbedPayload));

            public static Link Previous(ReadStreamPage page, ReadStreamOptions options)
                => new Link(
                    Constants.Relations.Previous,
                    LinkFormatter.FormatBackwardLink(
                        page.StreamId,
                        options.MaxCount,
                        page.Messages.Min(m => m.StreamVersion) - 1,
                        options.EmbedPayload));

            public static Link Next(ReadStreamPage page, ReadStreamOptions options)
                => new Link(
                    Constants.Relations.Next,
                    LinkFormatter.FormatForwardLink(
                        page.StreamId,
                        options.MaxCount,
                        page.Messages.Max(m => m.StreamVersion) + 1,
                        options.EmbedPayload));

            public static Link Last(ReadStreamPage page, ReadStreamOptions options)
                => new Link(
                    Constants.Relations.Last,
                    LinkFormatter.FormatBackwardLink(
                        page.StreamId,
                        options.MaxCount,
                        StreamVersion.End,
                        options.EmbedPayload));

            public static Link Self(ReadStreamOptions options) => new Link(
                Constants.Relations.Self,
                options.Self);

            public static Link Self(AppendStreamOptions options) => new Link(
                Constants.Relations.Self,
                $"{options.StreamId}");

            public static Link Feed(ReadStreamPage page, ReadStreamOptions options)
                => new Link(Constants.Relations.Feed, Last(page, options).Href);

            public static Link Feed(ReadStreamMessageOptions options)
                => new Link(
                    Constants.Relations.Feed,
                    LinkFormatter.FormatBackwardLink(
                        options.StreamId,
                        Constants.MaxCount,
                        StreamVersion.End,
                        false));

            public static Link Feed(AppendStreamOptions options)
                => new Link(
                    Constants.Relations.Feed,
                    LinkFormatter.FormatBackwardLink(
                        options.StreamId,
                        Constants.MaxCount,
                        StreamVersion.End,
                        false));

            public static IEnumerable<Link> Navigation(ReadStreamPage page, ReadStreamOptions options)
            {
                var first = First(page, options);

                var last = Last(page, options);

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