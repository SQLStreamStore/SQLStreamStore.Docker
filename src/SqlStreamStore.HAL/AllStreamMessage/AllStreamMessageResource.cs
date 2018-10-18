namespace SqlStreamStore.HAL.AllStreamMessage
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.HAL.Resources;
    using SqlStreamStore.Streams;

    internal class AllStreamMessageResource : IResource
    {
        private readonly IStreamStore _streamStore;

        public AllStreamMessageResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
        }

        public async Task<Response> Get(
            ReadAllStreamMessageOperation operation,
            CancellationToken cancellationToken)
        {
            var message = await operation.Invoke(_streamStore, cancellationToken);

            var links = TheLinks.RootedAt("../")
                .Index()
                .Find()
                .Add(Constants.Relations.Message, $"stream/{message.Position}").Self()
                .Add(
                    Constants.Relations.Feed,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        Constants.MaxCount,
                        Position.End,
                        false));

            if(message.MessageId == Guid.Empty)
            {
                return new Response(
                    new HALResponse(null)
                        .AddLinks(links),
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
                    payload,
                    metadata = message.JsonMetadata
                }).AddLinks(links));
        }
    }
}