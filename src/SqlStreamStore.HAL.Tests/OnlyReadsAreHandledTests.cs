namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using Xunit;
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

    public class OnlyReadsAreHandledTests : IDisposable
    {
        private static readonly HttpMethod[] s_methods =
        {
            HttpMethod.Delete,
            HttpMethod.Options,
            HttpMethod.Post,
            HttpMethod.Put,
            HttpMethod.Trace,
            new HttpMethod("PATCH")
        };

        private readonly IStreamStore _streamStore;
        private readonly AppFunc _methodNotAllowed;

        public OnlyReadsAreHandledTests()
        {
            _streamStore = new InMemoryStreamStore();
            _methodNotAllowed = env =>
            {
                var context = new OwinContext(env);
                context.Response.StatusCode = 405;

                return Task.CompletedTask;
            };
        }

        public static IEnumerable<object[]> StreamCases()
            => from method in s_methods
                from path in new[] { "", "/1", "/1/1" }
                select new object[] { method, path };

        [Theory, MemberData(nameof(StreamCases))]
        public async Task non_supported_method_on_stream(HttpMethod method, string path)
        {
            using(var fixture = new MiddlewareFixture(
                ReadStreamMiddleware.UseStreamStore(_streamStore)(_methodNotAllowed)))
            {
                var response = await fixture.HttpClient.SendAsync(new HttpRequestMessage(method, path));

                Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            }
        }

        public static IEnumerable<object[]> AllStreamCases()
            => from method in s_methods
                from path in new[] { "", "/1" }
                select new object[] { method, path };


        [Theory, MemberData(nameof(AllStreamCases))]
        public async Task non_supported_method_on_all_stream(HttpMethod method, string path)
        {
            using(var fixture = new MiddlewareFixture(
                ReadAllStreamMiddleware.UseStreamStore(_streamStore)(_methodNotAllowed)))
            {
                var response = await fixture.HttpClient.SendAsync(new HttpRequestMessage(method, path));

                Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            }
        }


        public void Dispose() => _streamStore.Dispose();
    }
}