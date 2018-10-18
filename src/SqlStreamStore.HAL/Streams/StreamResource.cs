namespace SqlStreamStore.HAL.Streams
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.HAL.Resources;
    using SqlStreamStore.Streams;

    internal class StreamResource : IResource
    {
        private readonly IStreamStore _streamStore;
        private readonly string _relativePathToRoot;
        private readonly SchemaSet<StreamResource> _schema;

        public StreamResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
            _relativePathToRoot = "../";
            _schema = new SchemaSet<StreamResource>();
        }

        private HALResponse AppendToStream => _schema.GetSchema(nameof(AppendToStream));
        private HALResponse DeleteStream => _schema.GetSchema(nameof(DeleteStream));

        public async Task<Response> Post(
            AppendStreamOperation operation,
            CancellationToken cancellationToken)
        {
            var result = await operation.Invoke(_streamStore, cancellationToken);

            var response = new Response(
                new HALResponse(result)
                    .AddLinks(Links.Self(operation))
                    .AddLinks(Links.Feed(operation))
                    .AddLinks(Links.Find()),
                result.CurrentVersion == 0
                    ? 201
                    : 200);
            if(operation.ExpectedVersion == ExpectedVersion.NoStream)
            {
                response.Headers[Constants.Headers.Location] =
                    new[] { $"{_relativePathToRoot}streams/{operation.StreamId}" };
            }

            return response;
        }

        public async Task<Response> Get(ReadStreamOperation operation, CancellationToken cancellationToken)
        {
            if(!operation.IsUriCanonical)
            {
                return new Response(new HALResponse(null), 308)
                {
                    Headers = { [Constants.Headers.Location] = new[] { $"../{operation.Self}" } }
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
                        page.LastStreamVersion,
                        page.LastStreamPosition,
                        page.FromStreamVersion,
                        page.NextStreamVersion,
                        page.IsEnd
                    })
                    .AddLinks(TheLinks
                        .RootedAt(_relativePathToRoot)
                        .Index()
                        .Find()
                        .StreamsNavigation(page, operation))
                    .AddEmbeddedResource(
                        Constants.Relations.AppendToStream,
                        AppendToStream)
                    .AddEmbeddedResource(
                        Constants.Relations.Delete,
                        DeleteStream)
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

            if(page.TryGetETag(out var eTag))
            {
                response.Headers.Add(eTag);
            }

            return response;
        }

        public async Task<Response> Delete(DeleteStreamOperation operation, CancellationToken cancellationToken)
        {
            await operation.Invoke(_streamStore, cancellationToken);

            return new Response(new HALResponse(new object()));
        }

        private static class Links
        {
            public static Link Find() => SqlStreamStore.HAL.Links.Find("../streams/{streamId}");

            public static Link Self(AppendStreamOperation operation) => new Link(
                Constants.Relations.Self,
                $"{operation.StreamId}");

            public static Link Feed(AppendStreamOperation operation)
                => new Link(
                    Constants.Relations.Feed,
                    LinkFormatter.FormatBackwardLink(
                        operation.StreamId,
                        Constants.MaxCount,
                        StreamVersion.End,
                        false));

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