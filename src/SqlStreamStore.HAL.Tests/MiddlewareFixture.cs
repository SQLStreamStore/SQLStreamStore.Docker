namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using SqlStreamStore.Streams;

    public class MiddlewareFixture : IDisposable
    {
        public MiddlewareFixture()
        {
            StreamStore = new InMemoryStreamStore();
            HttpClient = new HttpClient(
                new OwinHttpMessageHandler(SqlStreamStoreHalMiddleware.UseSqlStreamStoreHal(StreamStore)))
            {
                BaseAddress = new UriBuilder().Uri,
                DefaultRequestHeaders = { Accept = { new MediaTypeWithQualityHeaderValue("application/hal+json") } }
            };
        }

        public IStreamStore StreamStore { get; }

        public HttpClient HttpClient { get; }

        public void Dispose()
        {
            HttpClient.Dispose();
            StreamStore.Dispose();
        }

        public Task<AppendResult> WriteNMessages(string streamId, int n)
            => StreamStore.AppendToStream(
                streamId,
                ExpectedVersion.Any,
                Enumerable.Range(0, n)
                    .Select(_ => new NewStreamMessage(Guid.NewGuid(), "type", "{}", "{}"))
                    .ToArray());
    }
}