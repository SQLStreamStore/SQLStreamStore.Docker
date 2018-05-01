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
            public static Link SelfFeed(ReadAllStreamOptions options)
                => new Link(Constants.Relations.Self, options.Self);

            public static Link Self(Streams.StreamMessage message) => new Link(
                Constants.Relations.Self,
                $"streams/{message.StreamId}/{message.StreamVersion}");

            public static Link SelfAll(Streams.StreamMessage message)
                => new Link(Constants.Relations.Self, $"/{Constants.Streams.All}/{message.Position}");

            public static Link First(ReadAllStreamOptions options)
                => new Link(
                    Constants.Relations.First,
                    LinkFormatter.FormatForwardLink(
                        Constants.Streams.All,
                        options.MaxCount,
                        Position.Start,
                        options.EmbedPayload));

            public static Link Last(ReadAllStreamOptions options)
                => new Link(
                    Constants.Relations.Last,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        options.MaxCount,
                        Position.End,
                        options.EmbedPayload));

            public static Link Last(ReadAllStreamMessageOptions options)
                => new Link(
                    Constants.Relations.Last,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        Constants.MaxCount,
                        Position.End,
                        false));

            public static Link Feed(ReadAllStreamOptions options)
                => new Link(Constants.Relations.Feed, Last(options).Href);

            public static Link Feed(ReadAllStreamMessageOptions options)
                => new Link(Constants.Relations.Feed, Last(options).Href);

            public static Link Previous(ReadAllPage page, ReadAllStreamOptions options)
                => new Link(
                    Constants.Relations.Previous,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        options.MaxCount,
                        page.Messages.Min(m => m.Position) - 1,
                        options.EmbedPayload));

            public static Link Next(ReadAllPage page, ReadAllStreamOptions options)
                => new Link(
                    Constants.Relations.Next,
                    LinkFormatter.FormatForwardLink(
                        Constants.Streams.All,
                        options.MaxCount,
                        page.Messages.Max(m => m.Position) + 1,
                        options.EmbedPayload));

            public static IEnumerable<Link> Navigation(ReadAllPage page, ReadAllStreamOptions options)
            {
                var first = First(options);
                var last = Last(options);

                yield return first;

                if(options.Self != first.Href && !page.IsEnd)
                    yield return Previous(page, options);

                if(options.Self != last.Href && !page.IsEnd)
                    yield return Next(page, options);

                yield return last;
            }
        }

        internal static class Stream
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

            public static Link Metadata(ReadStreamOptions options)
                => new Link(
                    Constants.Relations.Metadata,
                    $"{options.StreamId}/metadata");

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

        internal static class StreamMessage
        {
            public static Link Self(ReadStreamMessageOptions options) => new Link(
                Constants.Relations.Self,
                $"{options.StreamVersion}");

            public static Link Self(Streams.StreamMessage message) => new Link(
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
                Streams.StreamMessage message = default(Streams.StreamMessage))
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
    }
}