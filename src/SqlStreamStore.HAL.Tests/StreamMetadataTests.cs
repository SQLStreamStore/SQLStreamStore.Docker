﻿namespace SqlStreamStore.HAL.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Shouldly;
    using SqlStreamStore.Streams;
    using Xunit;

    public class StreamMetadataTests
    {
        private const string StreamId = "a-stream";

        private readonly SqlStreamStoreHalMiddlewareFixture _fixture;

        public StreamMetadataTests()
        {
            _fixture = new SqlStreamStoreHalMiddlewareFixture();
        }

        [Fact]
        public async Task get_metadata_when_metadata_stream_does_not_exist()
        {
            using(var response =
                await _fixture.HttpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, $"/streams/{StreamId}/metadata")))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

                var resource = await response.AsHal();

                ((string) resource.State.streamId).ShouldBe(StreamId);
                ((int) resource.State.metadataStreamVersion).ShouldBe(ExpectedVersion.NoStream);
                ((int?) resource.State.maxAge).ShouldBe(default(int?));
                ((int?) resource.State.maxCount).ShouldBe(default(int?));
                ((string) resource.State.metadataJson).ShouldBeNull();

                resource.ShouldLink(Constants.Relations.Self, "metadata");
                resource.ShouldLink(Constants.Relations.Feed, "../");
            }
        }

        [Fact]
        public async Task get_metadata_when_metadata_stream_does_exist()
        {
            using(await _fixture.HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, $"/streams/{StreamId}/metadata")
                {
                    Content = new StringContent(JObject.FromObject(new
                    {
                        maxAge = 30,
                        maxCount = 20,
                        metadataJson = new
                        {
                            type = "a-type"
                        }
                    }).ToString())
                    {
                        Headers =
                        {
                            ContentType = new MediaTypeHeaderValue("application/json")
                        }
                    }
                }))
            using(var response =
                await _fixture.HttpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, $"/streams/{StreamId}/metadata")))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);

                var resource = await response.AsHal();

                ((string) resource.State.streamId).ShouldBe(StreamId);
                ((int) resource.State.metadataStreamVersion).ShouldBe(0);
                ((int?) resource.State.maxAge).ShouldBe(30);
                ((int?) resource.State.maxCount).ShouldBe(20);
                JToken.DeepEquals(JObject.Parse((string) resource.State.metadataJson),
                    JObject.FromObject(new
                    {
                        type = "a-type"
                    })).ShouldBeTrue();

                resource.ShouldLink(Constants.Relations.Self, "metadata");
                resource.ShouldLink(Constants.Relations.Feed, "../");
            }
        }

        [Fact]
        public async Task set_metadata()
        {
            using(var response = await _fixture.HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, $"/streams/{StreamId}/metadata")
                {
                    Content = new StringContent(JObject.FromObject(new
                    {
                        maxAge = 30,
                        maxCount = 20,
                        metadataJson = new
                        {
                            type = "a-type"
                        }
                    }).ToString())
                    {
                        Headers =
                        {
                            ContentType = new MediaTypeHeaderValue("application/json")
                        }
                    }
                }))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);

                var resource = await response.AsHal();

                ((string) resource.State.streamId).ShouldBe(StreamId);
                ((int?) resource.State.maxAge).ShouldBe(30);
                ((int?) resource.State.maxCount).ShouldBe(20);
                JToken.DeepEquals(JObject.Parse((string) resource.State.metadataJson),
                    JObject.FromObject(new
                    {
                        type = "a-type"
                    })).ShouldBeTrue();

                resource.ShouldLink(Constants.Relations.Self, "metadata");
                resource.ShouldLink(Constants.Relations.Feed, "../");
            }
        }

        private static IEnumerable<int[]> WrongExpectedVersions()
        {
            yield return new[] { ExpectedVersion.NoStream, ExpectedVersion.NoStream };
            yield return new[] { ExpectedVersion.NoStream, 2 };
        }

        public static IEnumerable<object[]> WrongExpectedVersionCases()
            => WrongExpectedVersions().Select(s => new object[] { s });

        [Theory]
        [MemberData(nameof(WrongExpectedVersionCases))]
        public async Task set_wrong_expected_version(int[] expectedVersions)
        {
            for(var i = 0; i < expectedVersions.Length - 1; i++)
            {
                using(await _fixture.HttpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Post, $"/streams/{StreamId}/metadata")
                    {
                        Headers =
                        {
                            { Constants.Headers.ExpectedVersion, $"{expectedVersions[i]}" }
                        },
                        Content = new StringContent(JObject.FromObject(new
                        {
                            maxAge = 30,
                            maxCount = 20,
                            metadataJson = new
                            {
                                type = "a-type"
                            }
                        }).ToString())
                        {
                            Headers =
                            {
                                ContentType = new MediaTypeHeaderValue("application/json")
                            }
                        }
                    }))
                { }
            }

            using(var response = await _fixture.HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, $"/streams/{StreamId}/metadata")
                {
                    Headers =
                    {
                        { Constants.Headers.ExpectedVersion, $"{expectedVersions[expectedVersions.Length - 1]}" }
                    },
                    Content = new StringContent(JObject.FromObject(new
                    {
                        maxAge = 30,
                        maxCount = 20,
                        metadataJson = new
                        {
                            type = "a-type"
                        }
                    }).ToString())
                    {
                        Headers =
                        {
                            ContentType = new MediaTypeHeaderValue("application/json")
                        }
                    }
                }))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
                response.Content.Headers.ContentType.ShouldBe(new MediaTypeHeaderValue(
                    Constants.Headers.ContentTypes.HalJson));
            }

            var page = await _fixture.StreamStore.ReadStreamForwards($"$${StreamId}", 0, int.MaxValue);

            page.Status.ShouldBe(PageReadStatus.Success);
            page.Messages.Length.ShouldBe(expectedVersions.Length - 1);
        }
    }
}