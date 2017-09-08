namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using SqlStreamStore.Streams;
    using MidFunc = System.Func<
        System.Func<
            System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task>,
        System.Func<
            System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task>>;

    public class SqlStreamStoreHalMiddlewareFixture : IDisposable
    {
        private readonly MiddlewareFixture _inner;
        public IStreamStore StreamStore { get; }
        public HttpClient HttpClient => _inner.HttpClient;

        public SqlStreamStoreHalMiddlewareFixture()
        {
           StreamStore = new InMemoryStreamStore();
            _inner = new MiddlewareFixture(SqlStreamStoreHalMiddleware.UseSqlStreamStoreHal(StreamStore));
        }

        public void Dispose()
        {
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