namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Shouldly;
    using SqlStreamStore.Streams;
    using Xunit;

    public class StreamAppendTests : IDisposable
    {
        private readonly SqlStreamStoreHalMiddlewareFixture _fixture;

        public StreamAppendTests()
        {
            _fixture = new SqlStreamStoreHalMiddlewareFixture();
        }

        [Fact]
        public async Task append_expected_version_any()
        {
            var messageId = Guid.NewGuid();

            var jsonData = JObject.FromObject(new
            {
                property = "value"
            });

            var jsonMetadata = JObject.FromObject(new
            {
                property = "metaValue"
            });
            
            using(var response = await _fixture.HttpClient.PostAsync(
                "/streams/a-stream",
                new StringContent(JObject.FromObject(new
                {
                    expectedVersion = ExpectedVersion.Any,
                    messages = new[]
                    {
                        new
                        {
                            messageId,
                            type = "type",
                            jsonData,
                            jsonMetadata
                        }
                    } 
                }).ToString())
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);
            }

            var page = await _fixture.StreamStore.ReadStreamForwards("a-stream", 0, 1);
            
            page.Status.ShouldBe(PageReadStatus.Success);
            page.Messages.Length.ShouldBe(1);
            page.Messages[0].MessageId.ShouldBe(messageId);
            page.Messages[0].Type.ShouldBe("type");
            JToken.DeepEquals(JObject.Parse(await page.Messages[0].GetJsonData()), jsonData).ShouldBeTrue();
            JToken.DeepEquals(JObject.Parse(page.Messages[0].JsonMetadata), jsonMetadata).ShouldBeTrue();
        }

        [Fact]
        public async Task append_expected_version_no_stream()
        {
            var messageId = Guid.NewGuid();

            var jsonData = JObject.FromObject(new
            {
                property = "value"
            });

            var jsonMetadata = JObject.FromObject(new
            {
                property = "metaValue"
            });
            
            using(var response = await _fixture.HttpClient.PostAsync(
                "/streams/a-stream",
                new StringContent(JObject.FromObject(new
                {
                    expectedVersion = ExpectedVersion.NoStream,
                    messages = new[]
                    {
                        new
                        {
                            messageId,
                            type = "type",
                            jsonData,
                            jsonMetadata
                        }
                    } 
                }).ToString())
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.Created);
                response.Headers.Location.ToString().ShouldBe("streams/a-stream");
            }

            var page = await _fixture.StreamStore.ReadStreamForwards("a-stream", 0, 1);
            
            page.Status.ShouldBe(PageReadStatus.Success);
            page.Messages.Length.ShouldBe(1);
            page.Messages[0].MessageId.ShouldBe(messageId);
            page.Messages[0].Type.ShouldBe("type");
            JToken.DeepEquals(JObject.Parse(await page.Messages[0].GetJsonData()), jsonData).ShouldBeTrue();
            JToken.DeepEquals(JObject.Parse(page.Messages[0].JsonMetadata), jsonMetadata).ShouldBeTrue();
        }

        [Theory]
        [InlineData(new[]{ExpectedVersion.NoStream, ExpectedVersion.NoStream})]
        [InlineData(new[]{ExpectedVersion.NoStream, 2})]
        public async Task wrong_expected_version(int[] expectedVersions)
        {
            var jsonData = JObject.FromObject(new
            {
                property = "value"
            });

            var jsonMetadata = JObject.FromObject(new
            {
                property = "metaValue"
            });

            for(var i = 0; i < expectedVersions.Length - 1; i++)
            {
                using(await _fixture.HttpClient.PostAsync(
                    "/streams/a-stream",
                    new StringContent(JObject.FromObject(new
                    {
                        expectedVersion = expectedVersions[i],
                        messages = new[]
                        {
                            new
                            {
                                messageId = Guid.NewGuid(),
                                type = "type",
                                jsonData,
                                jsonMetadata
                            }
                        }
                    }).ToString())
                    {
                        Headers =
                        {
                            ContentType = new MediaTypeHeaderValue("application/json")
                        }
                    }))
                { }
            }

            using(var response = await _fixture.HttpClient.PostAsync(
                "/streams/a-stream",
                new StringContent(JObject.FromObject(new
                {
                    expectedVersion = expectedVersions[expectedVersions.Length-1],
                    messages = new[]
                    {
                        new
                        {
                            messageId = Guid.NewGuid(),
                            type = "type",
                            jsonData,
                            jsonMetadata
                        }
                    }
                }).ToString())
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
                response.Content.Headers.ContentType.ShouldBe(new MediaTypeHeaderValue("application/problem+json"));
            }
            
        }

        public void Dispose() => _fixture.Dispose();
    }
}