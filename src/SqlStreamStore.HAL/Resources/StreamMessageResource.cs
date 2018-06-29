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

    internal class StreamMessageResource : IResource
    {
        private readonly IStreamStore _streamStore;

        public HttpMethod[] Options { get; } =
        {
            HttpMethod.Get,
            HttpMethod.Head,
            HttpMethod.Options
        };

        public StreamMessageResource(IStreamStore streamStore)
        {
            if(streamStore == null)
                throw new ArgumentNullException(nameof(streamStore));
            _streamStore = streamStore;
        }

        public async Task<Response> GetMessage(
            ReadStreamMessageByStreamVersionOperation operation,
            CancellationToken cancellationToken)
        {
            var message = await operation.Invoke(_streamStore, cancellationToken);

            if(message.MessageId == Guid.Empty)
            {
                return new Response(
                    new HALResponse(new
                        {
                            operation.StreamId,
                            operation.StreamVersion
                        })
                        .AddLinks(Links.Self(operation))
                        .AddLinks(Links.Navigation(operation))
                        .AddLinks(Links.Message(operation)),
                    404);
            }

            if(operation.StreamVersion == StreamVersion.End)
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
                        payload,
                        metadata = message.JsonMetadata
                    })
                    .AddEmbeddedResource(
                        Constants.Relations.Delete,
                        Schemas.DeleteStreamMessage)
                    .AddLinks(Links.Self(operation))
                    .AddLinks(Links.Navigation(operation, message))
                    .AddLinks(Links.Message(operation)));
        }

        public async Task<Response> DeleteMessage(
            DeleteStreamMessageOperation operation,
            CancellationToken cancellationToken)
        {
            await operation.Invoke(_streamStore, cancellationToken);

            return new Response(
                new HALResponse(new HALModelConfig()));
        }

        private static class Links
        {
            public static Link Self(ReadStreamMessageByStreamVersionOperation operation) => new Link(
                Constants.Relations.Self,
                $"{operation.StreamVersion}");

            public static Link Message(ReadStreamMessageByStreamVersionOperation operation) => new Link(
                Constants.Relations.Message,
                $"{operation.StreamVersion}");

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

            private static Link First() => new Link(Constants.Relations.First, $"{0}");

            private static Link Previous(ReadStreamMessageByStreamVersionOperation operation) => new Link(
                Constants.Relations.Previous,
                $"{operation.StreamVersion - 1}");

            private static Link Next(ReadStreamMessageByStreamVersionOperation operation) => new Link(
                Constants.Relations.Next,
                $"{operation.StreamVersion + 1}");

            private static Link Last() => new Link(Constants.Relations.Last, $"{-1}");

            private static Link Feed(ReadStreamMessageByStreamVersionOperation operation) => new Link(
                Constants.Relations.Feed,
                LinkFormatter.FormatBackwardLink(
                    $"../{operation.StreamId}",
                    Constants.MaxCount,
                    StreamVersion.End,
                    false));

            public static IEnumerable<Link> Navigation(
                ReadStreamMessageByStreamVersionOperation operation,
                StreamMessage message = default(StreamMessage))
            {
                yield return First();

                if(operation.StreamVersion > 0)
                {
                    yield return Previous(operation);
                }

                if(message.MessageId != default(Guid))
                {
                    yield return Next(operation);
                }

                yield return Last();

                yield return Feed(operation);
            }
        }
    }
}