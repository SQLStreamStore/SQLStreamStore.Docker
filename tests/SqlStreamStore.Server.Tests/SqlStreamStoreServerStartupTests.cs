using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SqlStreamStore;
using SqlStreamStore.HAL;
using SqlStreamStore.Server;
using Xunit;

namespace SQLStreamStore.Server.Tests
{
    public class SqlStreamStoreServerStartupTests : IDisposable
    {
        private readonly InMemoryStreamStore _streamStore;
        private readonly IWebHost _host;
        private TestServer _server;
        private readonly HttpClient _httpClient;

        public SqlStreamStoreServerStartupTests()
        {
            _streamStore = new InMemoryStreamStore();

            _server = new TestServer(
                new WebHostBuilder()
                    .UseStartup(new SqlStreamStoreServerStartup(
                        _streamStore,
                        new SqlStreamStoreMiddlewareOptions
                        {
                            UseCanonicalUrls = false
                        })));

            _httpClient = new HttpClient(_server.CreateHandler())
            {
                BaseAddress = new UriBuilder().Uri
            };
        }

        [Fact]
        public async Task StartsUp()
        {
            using (await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/")
            {
                Headers = {Accept = {new MediaTypeWithQualityHeaderValue("application/hal+json")}}
            }))
            {
            }
        }

        public void Dispose()
        {
            _streamStore?.Dispose();
            _host?.Dispose();
            _httpClient?.Dispose();
        }
    }
}