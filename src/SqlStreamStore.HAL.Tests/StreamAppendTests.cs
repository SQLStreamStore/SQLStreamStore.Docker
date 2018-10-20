namespace SqlStreamStore.HAL.Tests
{
    using System;
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

    public class StreamAppendTests : IDisposable
    {
        private const string StreamId = "a-stream";
        private readonly SqlStreamStoreHalMiddlewareFixture _fixture;

        public StreamAppendTests()
        {
            _fixture = new SqlStreamStoreHalMiddlewareFixture();
        }

        public static IEnumerable<object[]> AppendCases()
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

            var bodies = new JToken[]
            {
                JObject.FromObject(new
                {
                    messageId,
                    type = "type",
                    jsonData,
                    jsonMetadata
                }),
                JArray.FromObject(new[]
                {
                    new
                    {
                        messageId,
                        type = "type",
                        jsonData,
                        jsonMetadata
                    }
                })
            };

            foreach(var body in bodies)
            {
                yield return new object[]
                {
                    body,
                    ExpectedVersion.Any,
                    HttpStatusCode.Created,
                    messageId,
                    jsonData,
                    jsonMetadata
                };
                yield return new object[]
                {
                    body,
                    default(int?),
                    HttpStatusCode.Created,
                    messageId,
                    jsonData,
                    jsonMetadata
                };
                yield return new object[]
                {
                    body,
                    ExpectedVersion.NoStream,
                    HttpStatusCode.Created,
                    messageId,
                    jsonData,
                    jsonMetadata
                };
            }
        }

        [Theory, MemberData(nameof(AppendCases))]
        public async Task expected_version(
            JToken body,
            int? expectedVersion,
            HttpStatusCode statusCode,
            Guid messageId,
            JObject jsonData,
            JObject jsonMetadata)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/streams/{StreamId}")
            {
                Content = new StringContent(body.ToString())
            };

            if(expectedVersion.HasValue)
            {
                request.Headers.Add(Constants.Headers.ExpectedVersion, $"{expectedVersion}");
            }

            using(var response = await _fixture.HttpClient.SendAsync(request))
            {
                response.StatusCode.ShouldBe(statusCode);
            }

            var page = await _fixture.StreamStore.ReadStreamForwards(StreamId, 0, int.MaxValue);
            
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
                using(await _fixture.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"/streams/{StreamId}")
                {
                    Headers =
                    {
                        {Constants.Headers.ExpectedVersion, $"{expectedVersions[i]}"}
                    },
                    Content = new StringContent(JObject.FromObject(new
                    {
                        messageId = Guid.NewGuid(),
                        type = "type",
                        jsonData,
                        jsonMetadata
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

            using(var response = await _fixture.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"/streams/{StreamId}")
            {
                Headers =
                {
                    {Constants.Headers.ExpectedVersion, $"{expectedVersions[expectedVersions.Length - 1]}"}
                },
                Content = new StringContent(JObject.FromObject(new
                {
                    messageId = Guid.NewGuid(),
                    type = "type",
                    jsonData,
                    jsonMetadata
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
                    Constants.MediaTypes.HalJson));
            }
            var page = await _fixture.StreamStore.ReadStreamForwards(StreamId, 0, int.MaxValue);
            
            page.Status.ShouldBe(PageReadStatus.Success);
            page.Messages.Length.ShouldBe(expectedVersions.Length - 1);
        }

        private static IEnumerable<string> MalformedRequests()
        {
            var messageId = Guid.NewGuid();
            
            const string type = "type";
            
            var jsonData = JObject.FromObject(new
            {
                property = "value"
            });

            var jsonMetadata = JObject.FromObject(new
            {
                property = "metaValue"
            });
            
            yield return string.Empty;
            yield return "{}";
            
            yield return JObject.FromObject(new
            {
                messageId = Guid.Empty,
                type,
                jsonData,
                jsonMetadata
            }).ToString();
            
            yield return JObject.FromObject(new
            {
                type,
                jsonData,
                jsonMetadata
            }).ToString();

            yield return JObject.FromObject(new
            {
                messageId,
                jsonData,
                jsonMetadata
            }).ToString();

            yield return $@"{{ ""messageId"": ""{messageId}"", ""type"": ""{type}"", ""jsonData"": {{ }}";
            yield return $@"{{ ""messageId"": ""{messageId}"", ""type"": ""{type}"", ""jsonMetadata"": {{ }}";
        }

        public static IEnumerable<object[]> MalformedRequestCases()
            => MalformedRequests().Select(s => new object[] { s });

        [Theory, MemberData(nameof(MalformedRequestCases))]
        public async Task malformed_request_body(string malformedRequest)
        {
            using(var response = await _fixture.HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, $"/streams/{StreamId}")
                {
                    Content = new StringContent(malformedRequest)
                    {
                        Headers = { ContentType = new MediaTypeHeaderValue(Constants.MediaTypes.HalJson) }
                    }
                }))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            }
        }

        public void Dispose() => _fixture.Dispose();
    }
}