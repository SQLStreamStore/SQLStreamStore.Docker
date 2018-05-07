namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Halcyon.HAL;
    using SqlStreamStore.Streams;

    internal static class Links
    {
        internal static class All
        {
            public static Link SelfFeed(ReadAllStreamOperation operation)
                => new Link(Constants.Relations.Self, operation.Self);

            public static Link Self(Streams.StreamMessage message) => new Link(
                Constants.Relations.Self,
                $"streams/{message.StreamId}/{message.StreamVersion}");

            public static Link SelfAll(Streams.StreamMessage message)
                => new Link(Constants.Relations.Self, $"/{Constants.Streams.All}/{message.Position}");

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

            public static Link Last(ReadAllStreamMessageOperation operation)
                => new Link(
                    Constants.Relations.Last,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        Constants.MaxCount,
                        Position.End,
                        false));

            public static Link Feed(ReadAllStreamOperation operation)
                => new Link(Constants.Relations.Feed, Last(operation).Href);

            public static Link Feed(ReadAllStreamMessageOperation operation)
                => new Link(Constants.Relations.Feed, Last(operation).Href);

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
        }

        internal static class Stream
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

            public static Link Feed(ReadStreamPage page, ReadStreamOperation operation)
                => new Link(Constants.Relations.Feed, Last(page, operation).Href);

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
        }

        internal static class StreamMessage
        {
            public static Link Self(ReadStreamMessageByStreamVersionOperation operation) => new Link(
                Constants.Relations.Self,
                $"{operation.StreamVersion}");

            public static Link Self(Streams.StreamMessage message) => new Link(
                Constants.Relations.Self,
                $"{message.StreamId}/{message.StreamVersion}");

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

            public static IEnumerable<Link> Navigation(
                ReadStreamMessageByStreamVersionOperation operation,
                Streams.StreamMessage message = default(Streams.StreamMessage))
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
            }
        }
    }
}