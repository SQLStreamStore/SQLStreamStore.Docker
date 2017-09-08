namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using AppFunc = System.Func<
        System.Collections.Generic.IDictionary<string, object>,
        System.Threading.Tasks.Task>;

    using MidFunc = System.Func<
        System.Func<
            System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task>,
        System.Func<
            System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task>>;

    public class MiddlewareFixture : IDisposable
    {
        private readonly OwinHttpMessageHandler _messageHandler;

        public MiddlewareFixture(AppFunc appFunc)
            : this(new OwinHttpMessageHandler(appFunc))
        {
        }

        public MiddlewareFixture(MidFunc midFunc)
            : this(new OwinHttpMessageHandler(midFunc))
        {
        }

        private MiddlewareFixture(OwinHttpMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;

            HttpClient = new HttpClient(_messageHandler)
            {
                BaseAddress = new UriBuilder().Uri,
                DefaultRequestHeaders = { Accept = { new MediaTypeWithQualityHeaderValue("application/hal+json") } }
            };
        }
        public HttpClient HttpClient { get; }

        public void Dispose()
        {
            _messageHandler.Dispose();
            HttpClient.Dispose();
        }
    }
}