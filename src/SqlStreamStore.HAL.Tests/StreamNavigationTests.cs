namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Shouldly;
    using SqlStreamStore.Streams;
    using Xunit;

    public class StreamNavigationTests : IDisposable
    {
        private const string FirstLinkQuery = "d=f&m=20&p=0&e=0";
        private const string LastLinkQuery = "d=b&m=20&p=-1&e=0";

        private readonly SqlStreamStoreHalMiddlewareFixture _fixture;

        public StreamNavigationTests()
        {
            _fixture = new SqlStreamStoreHalMiddlewareFixture();
        }

        public void Dispose() => _fixture.Dispose();

        public static IEnumerable<object[]> GetNoMessagesPagingCases()
        {
            yield return new object[] { "stream", "/", HttpStatusCode.OK };
            yield return new object[] { "a-stream", "/streams/", HttpStatusCode.NotFound };
        }

        private static bool IsAllStream(string path) => path == "/stream";

        [Theory, MemberData(nameof(GetNoMessagesPagingCases))]
        public async Task read_head_link_no_messages(string stream, string baseAddress, HttpStatusCode statusCode)
        {
            using(var firstResponse = await _fixture.HttpClient.GetAsync($"{baseAddress}{stream}"))
            using(var response = await _fixture.HttpClient.GetAsync($"{baseAddress}{firstResponse.Headers.Location}"))
            {
                response.StatusCode.ShouldBe(statusCode);
                var eTag = IsAllStream(stream)
                    ? ETag.FromPosition(Position.End)
                    : ETag.FromStreamVersion(StreamVersion.End);
                response.Headers.ETag.ShouldBe(new EntityTagHeaderValue(eTag));

                var resource = await response.AsHal();

                var links = TheLinks
                    .RootedAt("../")
                    .Index()
                    .Find()
                    .Add(Constants.Relations.Self, $"{stream}?{LastLinkQuery}")
                    .Add(Constants.Relations.Last, $"{stream}?{LastLinkQuery}")
                    .Add(Constants.Relations.First, $"{stream}?{FirstLinkQuery}")
                    .Add(Constants.Relations.Feed, $"{stream}?{LastLinkQuery}");

                if(!IsAllStream($"{baseAddress}{stream}"))
                {
                    links.Add(Constants.Relations.Metadata, $"{stream}/metadata");
                }

                resource.ShouldLink(links);
            }
        }

        public static IEnumerable<object[]> GetPagingCases()
        {
            yield return new object[] { "stream", "/" };
            yield return new object[] { "a-stream", "/streams/" };
        }

        [Theory, MemberData(nameof(GetPagingCases))]
        public async Task read_head_link_when_multiple_pages(string stream, string baseAddress)
        {
            var result = await _fixture.WriteNMessages("a-stream", 30);

            using(var firstResponse = await _fixture.HttpClient.GetAsync($"{baseAddress}{stream}"))
            {
                firstResponse.StatusCode.ShouldBe(HttpStatusCode.PermanentRedirect);

                using(var response =
                    await _fixture.HttpClient.GetAsync($"{baseAddress}{firstResponse.Headers.Location}"))
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.OK);
                    var eTag = IsAllStream(stream)
                        ? ETag.FromPosition(result.CurrentPosition)
                        : ETag.FromStreamVersion(result.CurrentVersion);
                    response.Headers.ETag.ShouldBe(new EntityTagHeaderValue(eTag));

                    var resource = await response.AsHal();

                    var links = TheLinks
                        .RootedAt("../")
                        .Index()
                        .Find()
                        .Add(Constants.Relations.Self, $"{stream}?{LastLinkQuery}")
                        .Add(Constants.Relations.Last, $"{stream}?{LastLinkQuery}")
                        .Add(Constants.Relations.Previous, $"{stream}?d=b&m=20&p=9&e=0")
                        .Add(Constants.Relations.First, $"{stream}?{FirstLinkQuery}")
                        .Add(Constants.Relations.Feed, $"{stream}?{LastLinkQuery}");

                    if(!IsAllStream($"{baseAddress}{stream}"))
                    {
                        links.Add(Constants.Relations.Metadata, $"{stream}/metadata");
                    }

                    resource.ShouldLink(links);
                }
            }
        }

        [Theory, MemberData(nameof(GetPagingCases))]
        public async Task read_first_link(string stream, string baseAddress)
        {
            var result = await _fixture.WriteNMessages("a-stream", 10);

            using(var response = await _fixture.HttpClient.GetAsync($"{baseAddress}{stream}?{FirstLinkQuery}"))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                var eTag = IsAllStream(stream)
                    ? ETag.FromPosition(result.CurrentPosition)
                    : ETag.FromStreamVersion(result.CurrentVersion);
                response.Headers.ETag.ShouldBe(new EntityTagHeaderValue(eTag));

                var resource = await response.AsHal();

                var links = TheLinks.RootedAt("../")
                    .Index()
                    .Find()
                    .Add(Constants.Relations.Self, $"{stream}?{FirstLinkQuery}")
                    .Add(Constants.Relations.Last, $"{stream}?{LastLinkQuery}")
                    .Add(Constants.Relations.First, $"{stream}?{FirstLinkQuery}")
                    .Add(Constants.Relations.Feed, $"{stream}?{FirstLinkQuery}");

                if(!IsAllStream($"{baseAddress}{stream}"))
                {
                    links.Add(Constants.Relations.Metadata, $"{stream}/metadata");
                }
                
                resource.ShouldLink(links);
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

                var links = TheLinks.RootedAt("../")
                    .Index()
                    .Find()
                    .Add(Constants.Relations.Self, $"{stream}?{FirstLinkQuery}")
                    .Add(Constants.Relations.Last, $"{stream}?{LastLinkQuery}")
                    .Add(Constants.Relations.Next, $"{stream}?d=f&m=20&p=20&e=0")
                    .Add(Constants.Relations.First, $"{stream}?{FirstLinkQuery}")
                    .Add(Constants.Relations.Feed, $"{stream}?{FirstLinkQuery}");

                if(!IsAllStream($"{baseAddress}{stream}"))
                {
                    links.Add(Constants.Relations.Metadata, $"{stream}/metadata");
                }

                resource.ShouldLink(links);
            }
        }

        [Fact]
        public async Task read_stream_should_include_the_last_position_and_version()
        {
            await _fixture.WriteNMessages("a-stream", 30);

            var page = await _fixture.StreamStore.ReadStreamForwards("a-stream", StreamVersion.Start, 10, false);

            using(var firstResponse = await _fixture.HttpClient.GetAsync("/streams/a-stream"))
            using(var response = await _fixture.HttpClient.GetAsync($"/streams/{firstResponse.Headers.Location}"))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);

                var resource = await response.AsHal();

                ((int) resource.State.lastStreamVersion).ShouldBe(page.LastStreamVersion);
                ((long) resource.State.lastStreamPosition).ShouldBe(page.LastStreamPosition);
            }
        }
    }
}