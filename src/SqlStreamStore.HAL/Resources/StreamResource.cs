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
            AppendStreamOptions options,
            CancellationToken cancellationToken)
        {
            var operation = options.GetAppendOperation();

            var result = await operation.Invoke(_streamStore, cancellationToken);

            var response = new Response(
                new HALResponse(result)
                    .AddLinks(Links.Stream.Self(options))
                    .AddLinks(Links.Stream.Feed(options)),
                result.CurrentVersion == 0
                    ? 201
                    : 200);
            if(options.ExpectedVersion == ExpectedVersion.NoStream)
            {
                response.Headers[Constants.Headers.Location] = new[] { $"streams/{options.StreamId}" };
            }
            return response;
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
                        page.LastStreamPosition,
                        page.FromStreamVersion,
                        page.NextStreamVersion,
                        page.IsEnd
                    })
                    .AddLinks(Links.Stream.Self(options))
                    .AddLinks(Links.Stream.Navigation(page, options))
                    .AddLinks(Links.Stream.Feed(page, options))
                    .AddLinks(Links.Stream.Metadata(options))
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

        public async Task<Response> Delete(DeleteStreamOptions options, CancellationToken cancellationToken)
        {
            var operation = options.GetDeleteOperation();

            await operation.Invoke(_streamStore, cancellationToken);
            
            return new Response(new HALResponse(new object()));
        }
    }
}