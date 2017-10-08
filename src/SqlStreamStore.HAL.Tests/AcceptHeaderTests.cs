namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    public class AcceptHeaderTests : IDisposable
    {
        private readonly SqlStreamStoreHalMiddlewareFixture _fixture;

        public AcceptHeaderTests()
        {
            _fixture = new SqlStreamStoreHalMiddlewareFixture();
        }

        public static IEnumerable<object[]> NotAcceptableCases()
        {
            var requestUris = new[] { "/stream", "/streams/a-stream", "/" };
            var methods = new[]
            {
                HttpMethod.Get, 
                HttpMethod.Head, 
                HttpMethod.Options, 
                HttpMethod.Post,
                HttpMethod.Delete
            };
            var mediaTypes = new[] { "text/html", "application/hal", "application/hal+xml" };

            return from requestUri in requestUris
                from method in methods
                from mediaType in mediaTypes
                select new object[] { requestUri, method, new MediaTypeWithQualityHeaderValue(mediaType) };
        }

        [Theory, MemberData(nameof(NotAcceptableCases))]
        public async Task accept_other_than_hal_json_are_not_acceptable(
            string requestUri,
            HttpMethod method,
            MediaTypeWithQualityHeaderValue mediaType)
        {
            using(var response = await _fixture.HttpClient.SendAsync(new HttpRequestMessage(method, requestUri)
            {
                Headers = { Accept = { mediaType } }
            }))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.NotAcceptable);
            }
        }

        public void Dispose() => _fixture.Dispose();
    }
}