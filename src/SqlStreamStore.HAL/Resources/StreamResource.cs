namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.Streams;

    internal class StreamResource : IResource
    {
        private readonly IStreamStore _streamStore;

        public HttpMethod[] Options { get; } =
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

        public async Task<Response> AppendMessages(
            AppendStreamOperation operation,
            CancellationToken cancellationToken)
        {
            var result = await operation.Invoke(_streamStore, cancellationToken);

            var response = new Response(
                new HALResponse(result)
                    .AddLinks(Links.Stream.Self(operation))
                    .AddLinks(Links.Stream.Feed(operation)),
                result.CurrentVersion == 0
                    ? 201
                    : 200);
            if(operation.ExpectedVersion == ExpectedVersion.NoStream)
            {
                response.Headers[Constants.Headers.Location] = new[] { $"streams/{operation.StreamId}" };
            }
            return response;
        }

        public async Task<Response> GetPage(ReadStreamOperation operation, CancellationToken cancellationToken)
        {
            var page = await operation.Invoke(_streamStore, cancellationToken);

            var payloads = await Task.WhenAll(page.Messages
                .Select(message => operation.EmbedPayload
                    ? message.GetJsonData(cancellationToken)
                    : Task.FromResult<string>(null))
                .ToArray());

            return new Response(
                new HALResponse(new
                    {
                        page.LastStreamVersion,
                        page.LastStreamPosition,
                        page.FromStreamVersion,
                        page.NextStreamVersion,
                        page.IsEnd
                    })
                    .AddLinks(Links.Stream.Self(operation))
                    .AddLinks(Links.Stream.Navigation(page, operation))
                    .AddLinks(Links.Stream.Feed(page, operation))
                    .AddLinks(Links.Stream.Metadata(operation))
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
                            }).AddLinks(Links.StreamMessage.Self(message)))),
                page.Status == PageReadStatus.StreamNotFound ? 404 : 200);
        }

        public async Task<Response> Delete(DeleteStreamOperation operation, CancellationToken cancellationToken)
        {
            await operation.Invoke(_streamStore, cancellationToken);
            
            return new Response(new HALResponse(new object()));
        }
    }
}