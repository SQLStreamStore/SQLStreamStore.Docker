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

    internal class StreamResource : IResource
    {
        private readonly IStreamStore _streamStore;

        public HttpMethod[] Allowed { get; } =
        {
            HttpMethod.Get,
            HttpMethod.Head,
            HttpMethod.Options,
            HttpMethod.Post,
            HttpMethod.Delete
        };

        public StreamResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
        }

        public async Task<Response> Post(
            AppendStreamOperation operation,
            CancellationToken cancellationToken)
        {
            var result = await operation.Invoke(_streamStore, cancellationToken);

            var response = new Response(
                new HALResponse(result)
                    .AddLinks(Links.Self(operation))
                    .AddLinks(Links.Feed(operation)),
                result.CurrentVersion == 0
                    ? 201
                    : 200);
            if(operation.ExpectedVersion == ExpectedVersion.NoStream)
            {
                response.Headers[Constants.Headers.Location] = new[] { $"streams/{operation.StreamId}" };
            }

            return response;
        }

        public async Task<Response> Get(ReadStreamOperation operation, CancellationToken cancellationToken)
        {
            var page = await operation.Invoke(_streamStore, cancellationToken);

            var streamMessages = page.Messages.OrderByDescending(m => m.Position).ToArray();

            var payloads = await Task.WhenAll(
                Array.ConvertAll(
                    streamMessages,
                    message => operation.EmbedPayload
                        ? message.GetJsonData(cancellationToken)
                        : SkippedPayload.Instance));

            return new Response(
                new HALResponse(new
                    {
                        page.LastStreamVersion,
                        page.LastStreamPosition,
                        page.FromStreamVersion,
                        page.NextStreamVersion,
                        page.IsEnd
                    })
                    .AddLinks(Links.Self(operation))
                    .AddLinks(Links.Navigation(page, operation))
                    .AddLinks(Links.Feed(operation))
                    .AddLinks(Links.Metadata(operation))
                    .AddLinks(Links.Index())
                    .AddEmbeddedResource(
                        Constants.Relations.AppendToStream,
                        Schemas.AppendToStream)
                    .AddEmbeddedResource(
                        Constants.Relations.Delete,
                        Schemas.DeleteStream)
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
                                    Links.Message.Feed(message)))),
                page.Status == PageReadStatus.StreamNotFound ? 404 : 200);
        }

        public async Task<Response> Delete(DeleteStreamOperation operation, CancellationToken cancellationToken)
        {
            await operation.Invoke(_streamStore, cancellationToken);

            return new Response(new HALResponse(new object()));
        }

        private static class Links
        {
            public static Link First(ReadStreamPage page, ReadStreamOperation operation)
                => new Link(
                    Constants.Relations.First,
                    LinkFormatter.FormatForwardLink(
                        page.StreamId,
                        operation.MaxCount,
                        StreamVersion.Start,
                        operation.EmbedPayload));

            public static Link Previous(ReadStreamPage page, ReadStreamOperation operation)
                => new Link(
                    Constants.Relations.Previous,
                    LinkFormatter.FormatBackwardLink(
                        page.StreamId,
                        operation.MaxCount,
                        page.Messages.Min(m => m.StreamVersion) - 1,
                        operation.EmbedPayload));

            public static Link Next(ReadStreamPage page, ReadStreamOperation operation)
                => new Link(
                    Constants.Relations.Next,
                    LinkFormatter.FormatForwardLink(
                        page.StreamId,
                        operation.MaxCount,
                        page.Messages.Max(m => m.StreamVersion) + 1,
                        operation.EmbedPayload));

            public static Link Last(ReadStreamPage page, ReadStreamOperation operation)
                => new Link(
                    Constants.Relations.Last,
                    LinkFormatter.FormatBackwardLink(
                        page.StreamId,
                        operation.MaxCount,
                        StreamVersion.End,
                        operation.EmbedPayload));

            public static Link Self(ReadStreamOperation operation) => new Link(
                Constants.Relations.Self,
                operation.Self);

            public static Link Self(AppendStreamOperation operation) => new Link(
                Constants.Relations.Self,
                $"{operation.StreamId}");

            public static Link Feed(ReadStreamOperation operation)
                => new Link(Constants.Relations.Feed, operation.Self);

            public static Link Feed(ReadStreamMessageByStreamVersionOperation operation)
                => new Link(
                    Constants.Relations.Feed,
                    LinkFormatter.FormatBackwardLink(
                        operation.StreamId,
                        Constants.MaxCount,
                        StreamVersion.End,
                        false));

            public static Link Feed(AppendStreamOperation operation)
                => new Link(
                    Constants.Relations.Feed,
                    LinkFormatter.FormatBackwardLink(
                        operation.StreamId,
                        Constants.MaxCount,
                        StreamVersion.End,
                        false));

            public static Link Metadata(ReadStreamOperation operation)
                => new Link(
                    Constants.Relations.Metadata,
                    $"{operation.StreamId}/metadata");

            public static Link Index()
                => new Link(
                    Constants.Relations.Index,
                    "..");

            public static IEnumerable<Link> Navigation(ReadStreamPage page, ReadStreamOperation operation)
            {
                var first = First(page, operation);

                var last = Last(page, operation);

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
                    $"{message.StreamId}/{message.StreamVersion}");

                public static Link Feed(StreamMessage message) => new Link(
                    Constants.Relations.Feed,
                    message.StreamId);
            }
        }
    }
}