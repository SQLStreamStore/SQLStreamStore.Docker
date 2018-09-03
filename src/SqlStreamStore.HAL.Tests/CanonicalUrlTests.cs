namespace SqlStreamStore.HAL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Shouldly;
    using SqlStreamStore.HAL.Resources;
    using Xunit;

    public class CanonicalUrlTests
    {
        private const string StreamId = "a-stream";
        private readonly SqlStreamStoreHalMiddlewareFixture _fixture;

        public CanonicalUrlTests()
        {
            _fixture = new SqlStreamStoreHalMiddlewareFixture();
        }

        private static IEnumerable<string> GetQueryStrings(bool forward, bool prefetch)
        {
            IEnumerable<string[]> GetPermutations(string[] items, int length)
                => length == 1
                    ? items.Select(item => new[] { item })
                    : GetPermutations(items, length - 1)
                        .SelectMany(permutation => items.Where(item => !permutation.Contains(item)),
                            (t1, t2) => t1.Concat(new[] { t2 }).ToArray())
            ;

            var d = $"d={(forward ? 'f' : 'b')}";
            var m = "m=20";
            var e = $"e={(prefetch ? '1' : '0')}";
            var p = "p=0";

            var parameters = new[]
            {
                d, m, e, p
            };

            return
                from set in GetPermutations(parameters, 4)
                select string.Join('&', set.Where(x => x != null));
        }

        private static IEnumerable<(string, Uri)> NonCanonical()
        {
            var formatters = new Dictionary<bool, Func<string, int, long, bool, string>>
            {
                [true] = LinkFormatter.FormatForwardLink,
                [false] = LinkFormatter.FormatBackwardLink
            };

            (string streamId, string path)[] streams =
            {
                (StreamId, $"/streams/{StreamId}"),
                (Constants.Streams.All, Constants.Streams.All)
            };

            return
                from _ in streams
                from prefetch in new[] { true, false }
                from forward in new[] { true, false }
                let format = formatters[forward]
                let canonicalUri = format(_.streamId, 20, 0, prefetch)
                from queryString in GetQueryStrings(forward, prefetch)
                    // query strings are supposed to be case sensitive!!
                    //.Concat(new[] { $"d={(forward ? 'F' : 'B')}&M=20&P=0{(prefetch ? "&E" : string.Empty)}" })
                where $"{_.streamId}?{queryString}" != canonicalUri
                select ($"{_.path}?{queryString}", new Uri(
                    canonicalUri,
                    UriKind.Relative));
        }

        public static IEnumerable<object[]> NonCanonicalUriCases()
            => NonCanonical()
                .Select(x => new object[] { x.Item1, x.Item2 })
                .ToArray();

        [Theory, MemberData(nameof(NonCanonicalUriCases))]
        public async Task non_canonical_uri(string requestUri, Uri expectedLocation)
        {
            using(var response = await _fixture.HttpClient.GetAsync(requestUri))
            {
                response.StatusCode.ShouldBe(HttpStatusCode.PermanentRedirect);
                response.Headers.Location.ShouldBe(expectedLocation);
            }
        }
    }
}