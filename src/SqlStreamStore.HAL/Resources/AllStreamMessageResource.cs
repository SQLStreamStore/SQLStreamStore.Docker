namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.Streams;

    internal class AllStreamMessageResource : IResource
    {
        private readonly IStreamStore _streamStore;

        public HttpMethod[] Allowed { get; } =
        {
            HttpMethod.Get,
            HttpMethod.Head,
            HttpMethod.Options
        };

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

            if(message.MessageId == Guid.Empty)
            {
                return new Response(
                    new HALResponse(new HALModelConfig())
                        .AddLinks(Links.Feed())
                        .AddLinks(Links.Find()),
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
                }).AddLinks(
                    Links.Self(message),
                    Links.Message(message),
                    Links.Feed(),
                    Links.Find()));
        }

        private static class Links
        {
            public static Link Find() => SqlStreamStore.HAL.Links.Find("../streams/{streamId}");

            public static Link Last()
                => new Link(
                    Constants.Relations.Last,
                    LinkFormatter.FormatBackwardLink(
                        $"../{Constants.Streams.All}",
                        Constants.MaxCount,
                        Position.End,
                        false));

            public static Link Self(StreamMessage message)
                => new Link(Constants.Relations.Self, $"{message.Position}");

            public static Link Message(StreamMessage message)
                => new Link(Constants.Relations.Message, $"{message.Position}");

            public static Link Feed()
                => new Link(Constants.Relations.Feed, Last().Href);
        }
    }
}