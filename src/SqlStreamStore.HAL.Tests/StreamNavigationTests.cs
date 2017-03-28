namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    public class StreamNavigationTests : IDisposable
    {
        private const string FirstLinkQuery = "d=f&m=20&p=0";
        private const string LastLinkQuery = "d=b&m=20&p=-1";

        private readonly MiddlewareFixture _fixture;

        public StreamNavigationTests()
        {
            _fixture = new MiddlewareFixture();
        }


        public void Dispose() => _fixture.Dispose();

        public static IEnumerable<object[]> GetPagingCases()
        {
            yield return new object[] { "stream", "/" };
            yield return new object[] { "a-stream", "/streams/" };
        }

        [Theory, MemberData(nameof(GetPagingCases))]
        public async Task read_head_link_no_messages(string stream, string baseAddress)
        {
            using(var response = await _fixture.HttpClient.GetAsync($"{baseAddress}{stream}"))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);

                var resource = await response.AsHal();

                resource.Links.Keys.ShouldBe(new[] { "self", "first", "last", "streamStore:feed" });

                resource.ShouldLink("self", $"{stream}?{LastLinkQuery}");

                resource.ShouldLink("last", $"{stream}?{LastLinkQuery}");

                resource.ShouldLink("first", $"{stream}?{FirstLinkQuery}");

                resource.ShouldLink("streamStore:feed", $"{stream}?{LastLinkQuery}");
            }
        }

        [Theory, MemberData(nameof(GetPagingCases))]
        public async Task read_head_link_when_multiple_pages(string stream, string baseAddress)
        {
            await _fixture.WriteNMessages("a-stream", 30);

            using(var response = await _fixture.HttpClient.GetAsync($"{baseAddress}{stream}"))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);

                var resource = await response.AsHal();

                resource.Links.Keys.ShouldBe(new[] { "self", "first", "previous", "last", "streamStore:feed" });

                resource.ShouldLink("self", $"{stream}?{LastLinkQuery}");

                resource.ShouldLink("last", $"{stream}?{LastLinkQuery}");

                resource.ShouldLink("previous", $"{stream}?d=b&m=20&p=9");

                resource.ShouldLink("first", $"{stream}?{FirstLinkQuery}");

                resource.ShouldLink("streamStore:feed", $"{stream}?{LastLinkQuery}");
            }
        }

        [Theory, MemberData(nameof(GetPagingCases))]
        public async Task read_first_link(string stream, string baseAddress)
        {
            await _fixture.WriteNMessages("a-stream", 10);

            using(var response = await _fixture.HttpClient.GetAsync($"{baseAddress}{stream}?{FirstLinkQuery}"))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);

                var resource = await response.AsHal();

                resource.Links.Keys.ShouldBe(new[] { "self", "first", "last", "streamStore:feed" });

                resource.ShouldLink("self", $"{stream}?{FirstLinkQuery}");

                resource.ShouldLink("last", $"{stream}?{LastLinkQuery}");

                resource.ShouldLink("first", $"{stream}?{FirstLinkQuery}");

                resource.ShouldLink("streamStore:feed", $"{stream}?{LastLinkQuery}");
            }
        }

        [Theory, MemberData(nameof(GetPagingCases))]
        public async Task read_first_link_when_multiple_pages(string stream, string baseAddress)
        {
            await _fixture.WriteNMessages("a-stream", 30);

            using(var response = await _fixture.HttpClient.GetAsync($"{baseAddress}{stream}?{FirstLinkQuery}"))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);

                var resource = await response.AsHal();

                resource.Links.Keys.ShouldBe(new[] { "self", "first", "next", "last", "streamStore:feed" });


                resource.ShouldLink("self", $"{stream}?{FirstLinkQuery}");

                resource.ShouldLink("last", $"{stream}?{LastLinkQuery}");

                resource.ShouldLink("next", $"{stream}?d=f&m=20&p=20");

                resource.ShouldLink("first", $"{stream}?{FirstLinkQuery}");

                resource.ShouldLink("streamStore:feed", $"{stream}?{LastLinkQuery}");
            }
        }
    }
}