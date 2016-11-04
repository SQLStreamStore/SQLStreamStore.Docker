using System.Linq;
using Jil;
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
                Embedded = new { Page = messages.Select(ToPageResponse).ToList() },
                Count = messages.Length
            };
        }

        public static object GetMessage(StreamMessage m)
        {
            return new
            {
                _links = Links.CreateItemLink(m.Position),
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

        static object ToPageResponse(StreamMessage m)
        {
            return new
            {
                _links = Links.CreateItemLink(m.Position),
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
            return new Links
            {
                Self = new { Href = path + $"?position={positionOfFirstEvent}&direction={(direction == 1 ? "forwards" : "backwards")}" },
                Next = numberOfEvents < pageSize ? null : new { Href = $"{path}?position={positionOfLastEvent + direction}&direction={(direction == 1 ? "forwards" : "backwards")}" }
            };
        }

        public static Links CreateItemLink(long position)
        {
            return new Links
            {
                Self = new { Href = "/stream/" + position },
            };
        }
    }
}