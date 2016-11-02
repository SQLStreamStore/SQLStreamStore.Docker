using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SqlStreamStore.Streams;

namespace SqlStreamStore.HAL
{
    class HalResponse
    {
        [JsonProperty(PropertyName = "_links")]
        public Links Links { get; private set; }

        public int Count { get; private set; }

        [JsonProperty(PropertyName = "_embedded")]
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
                Embedded = new { Page = messages.Select(ToResponse).ToList() },
                Count = messages.Length
            };
        }

        public static object GetMessage(StreamMessage message)
        {
            return ToResponse(message);
        }

        static object ToResponse(StreamMessage m)
        {
            return new
            {
                Links = Links.CreateItemLink(m.Position),
                m.Position,
                m.CreatedUtc,
                m.MessageId,
                JsonData = JsonConvert.DeserializeObject(m.JsonData),
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

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object Next { get; private set; }

        public static Links CreatePaginationLinks(string path, long positionOfFirstEvent, long positionOfLastEvent, int numberOfEvents, int pageSize, int direction)
        {
            return new Links
            {
                Self = new { Href = path + "?position=" + positionOfFirstEvent },
                Next = numberOfEvents < pageSize ? null : new { Href = path + "?position=" + positionOfLastEvent + direction }
            };
        }

        public static Links CreateItemLink(long position)
        {
            return new Links
            {
                Self = new { Href = "/streamMessage?position=" + position },
            };
        }
    }
}   