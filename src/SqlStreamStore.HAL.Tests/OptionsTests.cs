namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    public class OptionsTests : IDisposable
    {
        private readonly SqlStreamStoreHalMiddlewareFixture _fixture;

        public OptionsTests()
        {
            _fixture = new SqlStreamStoreHalMiddlewareFixture();
        }

        public void Dispose() => _fixture.Dispose();

        public static IEnumerable<object[]> OptionsAllowedMethodCases()
        {
            yield return new object[]
            {
                "/stream",
                new[] { HttpMethod.Get, HttpMethod.Head, HttpMethod.Options }
            };
            
            yield return new object[]
            {
                "/stream/123",
                new[] { HttpMethod.Get, HttpMethod.Head, HttpMethod.Options }
            };

            yield return new object[]
            {
                "/streams/a-stream",
                new[] { HttpMethod.Get, HttpMethod.Head, HttpMethod.Options, HttpMethod.Delete, HttpMethod.Post }
            };

            yield return new object[]
            {
                "/streams/a-stream/0",
                new[] { HttpMethod.Get, HttpMethod.Head, HttpMethod.Options }
            };
        }

        [Theory, MemberData(nameof(OptionsAllowedMethodCases))]
        public async Task options_returns_the_correct_cors_headers(string requestUri, HttpMethod[] allowedMethods)
        {
            using(var response = await _fixture.HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Options, requestUri)))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                response.Headers.GetValues("Access-Control-Allow-Headers")
                    .ShouldBe(new[] { "Content-Type", "X-Requested-With", "Authorization" }, true);
                response.Headers.GetValues("Access-Control-Allow-Origin")
                    .ShouldBe(new[] { "*" }, true);
                response.Headers.GetValues("Access-Control-Allow-Methods")
                    .ShouldBe(allowedMethods.Select(_ => _.Method), true);
            }
        }
    }
}