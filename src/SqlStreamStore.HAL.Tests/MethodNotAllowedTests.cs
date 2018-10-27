namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    public class MethodNotAllowedTests : IDisposable
    {
        private static readonly ImmutableHashSet<HttpMethod> s_AllMethods
            = ImmutableHashSet.Create(
                (from property in typeof(HttpMethod).GetProperties(BindingFlags.Public | BindingFlags.Static)
                    where property.PropertyType == typeof(HttpMethod)
                    select (HttpMethod) property.GetValue(null)).ToArray());

        private readonly SqlStreamStoreHalMiddlewareFixture _fixture;

        public MethodNotAllowedTests()
        {
            _fixture = new SqlStreamStoreHalMiddlewareFixture();
        }

        private static IEnumerable<(string requestUri, HttpMethod method, HttpMethod[] allowed)> CreateTestCases(
            string requestUri,
            params HttpMethod[] allowed)
            => s_AllMethods.Except(allowed).Select(method => (requestUri, method, allowed));

        public static IEnumerable<object[]> MethodNotAllowedCases()
            => CreateTestCases(
                    "/",
                    HttpMethod.Get,
                    HttpMethod.Head,
                    HttpMethod.Options)
                .Concat(CreateTestCases(
                    "/stream",
                    HttpMethod.Get,
                    HttpMethod.Head,
                    HttpMethod.Options))
                .Concat(CreateTestCases(
                    "/stream/0",
                    HttpMethod.Get,
                    HttpMethod.Head,
                    HttpMethod.Options))
                .Concat(CreateTestCases(
                    "/streams/a-stream",
                    HttpMethod.Get,
                    HttpMethod.Head,
                    HttpMethod.Options,
                    HttpMethod.Post,
                    HttpMethod.Delete))
                .Concat(CreateTestCases(
                    "/streams/a-stream/0",
                    HttpMethod.Get,
                    HttpMethod.Head,
                    HttpMethod.Options,
                    HttpMethod.Delete))
                .Concat(CreateTestCases(
                    $"/streams/a-stream/{Guid.Empty}",
                    HttpMethod.Get,
                    HttpMethod.Head,
                    HttpMethod.Options,
                    HttpMethod.Delete))
                .Concat(CreateTestCases(
                    "/streams/a-stream/metadata",
                    HttpMethod.Get,
                    HttpMethod.Head,
                    HttpMethod.Options,
                    HttpMethod.Post))
                .Concat(CreateTestCases(
                    "/docs/doc",
                    HttpMethod.Get,
                    HttpMethod.Head,
                    HttpMethod.Options))
                .Select(testCase => new object[] { testCase.requestUri, testCase.method, testCase.allowed });


        [Theory, MemberData(nameof(MethodNotAllowedCases))]
        public async Task incorrect_method_not_allowed(string requestUri, HttpMethod method, HttpMethod[] allowed)
        {
            using(var response = await _fixture.HttpClient.SendAsync(new HttpRequestMessage(method, requestUri)))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);

                response.Headers.TryGetValues(Constants.Headers.Allowed, out var allowedHeaders).ShouldBeTrue();

                allowedHeaders.ShouldBe(allowed.Select(x => x.Method), true);
            }
        }

        public void Dispose() => _fixture.Dispose();
    }
}