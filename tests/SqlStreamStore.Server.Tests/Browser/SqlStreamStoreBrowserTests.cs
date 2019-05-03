using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using SqlStreamStore.Server.Browser;
using Xunit;

namespace SqlStreamStore.Server.Tests.Browser
{
    public class SqlStreamStoreBrowserTests : IDisposable
    {
        private readonly TestServer _server;
        private readonly HttpClient _httpClient;

        public SqlStreamStoreBrowserTests()
        {
            _server = new TestServer(
                new WebHostBuilder()
                    .Configure(app => app.UseSqlStreamStoreBrowser(typeof(SqlStreamStoreBrowserTests))));

            _httpClient = new HttpClient(_server.CreateHandler())
            {
                BaseAddress = new UriBuilder().Uri
            };
        }

        public static IEnumerable<object[]> IndexPageCases()
        {
            yield return new object[] {"/"};
            yield return new object[] {"/stream"};
            yield return new object[] {"/streams/a-stream"};
            yield return new object[] {"/streams/a-stream/metadata"};
        }

        [Theory, MemberData(nameof(IndexPageCases))]
        public async Task RequestsForHtmlReturnTheIndexPage(string path)
        {
            using (var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, path)
            {
                Headers = {Accept = {new MediaTypeWithQualityHeaderValue("text/html")}}
            }))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(await GetStaticEmbeddedResource("index.html"), await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task RequestsForStaticFilesFromRootAreReturned()
        {
            using (var response = await _httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "/static/js/ws.js")
                {
                    Headers = {Accept = {new MediaTypeWithQualityHeaderValue("*/*")}}
                }))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(
                    await GetStaticEmbeddedResource("static.js.ws.js"),
                    await response.Content.ReadAsStringAsync());
            }
        }

        public static IEnumerable<object[]> StaticContentCases()
        {
            yield return new object[] {"/stream/", "../../../"};
            yield return new object[] {"/streams/a-stream/", "../../../../"};
            yield return new object[] {"/streams/a-stream/metadata/", "../../../../../"};
        }

        [Theory, MemberData(nameof(StaticContentCases))]
        public async Task RequestsForStaticAreRedirectedIfNotAtRoot(string path, string parent)
        {
            using (var response = await _httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, $"{path}static/js/ws.js")
                {
                    Headers = {Accept = {new MediaTypeWithQualityHeaderValue("*/*")}}
                }))
            {
                Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode);
                Assert.Equal($"{parent}static/js/ws.js", response.Headers.Location?.ToString());
            }
        }

        private static async Task<string> GetStaticEmbeddedResource(string resource)
        {
            using (var stream = typeof(SqlStreamStoreBrowserTests)
                .Assembly
                .GetManifestResourceStream(typeof(SqlStreamStoreBrowserTests), resource))
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _server?.Dispose();
        }
    }
}