namespace SqlStreamStore.HAL.AllStream
{
    using System.Linq;
    using SqlStreamStore.HAL.Resources;
    using SqlStreamStore.Streams;

    internal static class AllStreamLinkExtensions
    {
        public static TheLinks Navigation(
            this TheLinks links,
            ReadAllPage page,
            ReadAllStreamOperation operation)
        {
            var first = LinkFormatter.FormatForwardLink(
                Constants.Streams.All,
                operation.MaxCount,
                Position.Start,
                operation.EmbedPayload);

            var last = LinkFormatter.FormatBackwardLink(
                Constants.Streams.All,
                operation.MaxCount,
                Position.End,
                operation.EmbedPayload);

            links.Add(Constants.Relations.First, first);

            if(operation.Self != first && !page.IsEnd)
            {
                links.Add(
                    Constants.Relations.Previous,
                    LinkFormatter.FormatBackwardLink(
                        Constants.Streams.All,
                        operation.MaxCount,
                        page.Messages.Min(m => m.Position) - 1,
                        operation.EmbedPayload));
            }

            links.Add(Constants.Relations.Feed, operation.Self).Self();

            if(operation.Self != last && !page.IsEnd)
            {
                links.Add(
                    Constants.Relations.Next,
                    LinkFormatter.FormatForwardLink(
                        Constants.Streams.All,
                        operation.MaxCount,
                        page.Messages.Max(m => m.Position) + 1,
                        operation.EmbedPayload));
            }

            links.Add(Constants.Relations.Last, last);

            return links;
        }
    }
}