namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Halcyon.HAL;
    using SqlStreamStore.HAL.StreamBrowser;

    internal class StreamBrowserResource
    {
        private readonly IStreamStore _streamStore;

        public StreamBrowserResource(IStreamStore streamStore)
        {
            _streamStore = streamStore;
        }

        public async Task<Response> Get(ListStreamsOperation operation, CancellationToken cancellationToken)
        {
            var listStreamsPage = await operation.Invoke(_streamStore, cancellationToken);

            var halResponse = new HALResponse(new
                {
                    listStreamsPage.ContinuationToken
                })
                .AddEmbeddedCollection(
                    Constants.Relations.Feed,
                    Array.ConvertAll(
                        listStreamsPage.StreamIds,
                        streamId =>
                        {
                            var href = $"../../streams/{streamId}";
                            return new HALResponse(null)
                                .AddLinks(
                                    new Link(Constants.Relations.Self, href, streamId),
                                    new Link(Constants.Relations.Feed, href, streamId));
                        }
                    ));

            if(listStreamsPage.ContinuationToken != null)
            {
                halResponse.AddLinks(
                    new Link(
                        Constants.Relations.Next,
                        $"browser?p={operation.Pattern.Value}&t={operation.PatternType}&c={listStreamsPage.ContinuationToken}&m={operation.MaxCount}"));
            }

            return new HalJsonResponse(halResponse);
        }
    }
}