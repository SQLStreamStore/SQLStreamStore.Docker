using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using SqlStreamStore;
using SqlStreamStore.HAL;
using SqlStreamStore.Server;
using Xunit;

namespace SQLStreamStore.Server.Tests
{
    public class SqlStreamStoreServerStartupTests : IDisposable
    {
        private readonly InMemoryStreamStore _streamStore;
        private readonly TestServer _server;
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
            using (var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/")
            {
                Headers = {Accept = {new MediaTypeWithQualityHeaderValue("application/hal+json")}}
            }))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            using (var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/health/ready")))
            {
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }

            using (var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/health/live")))
            {
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
        }

        [Fact]
        public async Task ServiceUnavailableWhenNotReady()
        {
            _streamStore.Dispose();
            using (var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/health/ready")))
            {
                Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            }
        }        

        public void Dispose()
        {
            _streamStore?.Dispose();
            _server?.Dispose();
            _httpClient?.Dispose();
        }
    }
}