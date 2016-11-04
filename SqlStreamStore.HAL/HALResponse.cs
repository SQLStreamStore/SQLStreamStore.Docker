using System;
using System.Linq;
using Jil;
using Nancy.Helpers;
using SqlStreamStore.Streams;

namespace SqlStreamStore.HAL
{
    class HalResponse
    {
        [JilDirective(Name = "_links")]
        public Links Links { get; private set; }

        public int Count { get; private set; }

        [JilDirective(Name = "_embedded")]
        public object Embedded { get; private set; }

        public static HalResponse GetPage(StreamMessage[] messages, int pageSize, string path, int direction)
        {
            if (messages.Length <= 0)
            {
                return null;
            }

            var links = Links.CreatePaginationLinks(
                path,
                messages.First().Position,
                messages.Last().Position,
                messages.Length,
                pageSize,
                direction
            );

            return new HalResponse
            {
                Links = links,
                Embedded = new { Page = messages.Select(m => ToPageResponse(path, m)).ToList() },
                Count = messages.Length
            };
        }

        public static object GetMessage(string path, StreamMessage m)
        {
            return new
            {
                _links = Links.CreateItemLink(path, m.Position),
                m.Position,
                m.CreatedUtc,
                m.MessageId,
                JsonData = JSON.DeserializeDynamic(m.JsonData),
                m.JsonMetadata,
                m.StreamVersion,
                m.StreamId,
                m.Type
            };
        }

        static object ToPageResponse(string path, StreamMessage m)
        {
            return new
            {
                _links = Links.CreateItemLink(path, m.Position),
                m.Position,
                m.CreatedUtc,
                m.MessageId,
                m.JsonMetadata,
                m.StreamVersion,
                m.StreamId,
                m.Type
            };
        }
    }

    class Links
    {
        public object Self { get; private set; }

        public object Next { get; private set; }

        public static Links CreatePaginationLinks(string path, long positionOfFirstEvent, long positionOfLastEvent, int numberOfEvents, int pageSize, int direction)
        {
            var self = new Uri(path)
                .AddQuery("position", positionOfFirstEvent)
                .AddQuery("direction", direction == 1 ? "forwards" : "backwards")
                .ToString();

            var next = new Uri(path)
                .AddQuery("position", positionOfLastEvent + direction)
                .AddQuery("direction", direction == 1 ? "forwards" : "backwards")
                .ToString();

            return new Links
            {
                Self = new { Href = self },
                Next = numberOfEvents < pageSize ? null : new { Href = next }
            };
        }

        public static Links CreateItemLink(string path, long position)
        {
            var self = new Uri(path)
                .AddQuery("position", position)
                .ToString();

            return new Links
            {
                Self = new { Href = self }
            };
        }
    }

    public static class HttpExtensions
    {
        public static Uri AddQuery(this Uri uri, string name, object value)
        {
            var ub = new UriBuilder(uri);

            // decodes urlencoded pairs from uri.Query to HttpValueCollection
            var httpValueCollection = HttpUtility.ParseQueryString(uri.Query);

            httpValueCollection.Add(name, value.ToString());

            // urlencodes the whole HttpValueCollection
            ub.Query = httpValueCollection.ToString();

            return ub.Uri;
        }
    }
}