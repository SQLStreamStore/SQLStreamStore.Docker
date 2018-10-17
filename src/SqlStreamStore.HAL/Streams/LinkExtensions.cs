namespace SqlStreamStore.HAL.Streams
{
    using System.Linq;
    using SqlStreamStore.HAL.Resources;
    using SqlStreamStore.Streams;

    internal static class StreamsLinkExtensions
    {
        public static TheLinks StreamsNavigation(this TheLinks links, ReadStreamPage page, ReadStreamOperation operation)
        {
            var baseAddress = $"streams/{operation.StreamId}";

            var first = LinkFormatter.FormatForwardLink(
                baseAddress,
                operation.MaxCount,
                StreamVersion.Start,
                operation.EmbedPayload);

            var last = LinkFormatter.FormatBackwardLink(
                baseAddress,
                operation.MaxCount,
                StreamVersion.End,
                operation.EmbedPayload);

            links.Add(Constants.Relations.First, first);

            if(operation.Self != first && !page.IsEnd)
            {
                links.Add(
                    Constants.Relations.Previous,
                    LinkFormatter.FormatBackwardLink(
                        baseAddress,
                        operation.MaxCount,
                        page.Messages.Min(m => m.StreamVersion) - 1,
                        operation.EmbedPayload));
            }

            links.Add(Constants.Relations.Feed, operation.Self).Self();

            if(operation.Self != last && !page.IsEnd)
            {
                links.Add(
                    Constants.Relations.Next,
                    LinkFormatter.FormatForwardLink(
                        baseAddress,
                        operation.MaxCount,
                        page.Messages.Max(m => m.StreamVersion) + 1,
                        operation.EmbedPayload));
            }

            links.Add(Constants.Relations.Last, last)
                .Add(Constants.Relations.Feed, operation.Self).Self()
                .Add(Constants.Relations.Metadata,
                    $"{baseAddress}/metadata");

            return links;
        }
    }
}