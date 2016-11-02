using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SqlStreamStore.Streams;

namespace SqlStreamStore.HAL
{
    class HALResponse
    {
        [JsonProperty(PropertyName = "_links")]
        public Links Links { get; private set; }

        public int Count { get; set; }


        [JsonProperty(PropertyName = "_embedded")]
        public Embedded Embedded { get; private set; }

        public static HALResponse Create(StreamMessage[] messages, int pageSize, string path, int direction)
        {
            if (messages.Length <= 0)
            {
                return null;
            }

            var links = Links.CreateLinks(
                path,
                messages.First().Position,
                messages.Last().Position,
                messages.Length,
                pageSize,
                direction
            );

            return new HALResponse
            {
                Links = links,
                Embedded = new Embedded { Page = messages.Select(m => m).Cast<object>().ToList() },
                Count = messages.Length
            };
        }
    }

    class Links
    {
        public Link Self { get; private set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Link Next { get; private set; }

        public static Links CreateLinks(string path, long positionOfFirstEvent, long positionOfLastEvent, int numberOfEvents, int pageSize, int direction)
        {
            return new Links
            {
                Self = Link.CreateSelfLink(path, positionOfFirstEvent),
                Next = Link.CreateNextLink(path, positionOfLastEvent + direction, pageSize, numberOfEvents),
            };
        }
    }

    class Link
    {
        public string Href { get; }

        Link(string path, long position)
        {
            Href = path + "?position=" + position;
        }

        public static Link CreateSelfLink(string path, long position)
        {
            return new Link(path, position);
        }

        public static Link CreateNextLink(string path, long position, int pageSize, int actualPageSize)
        {
            if (actualPageSize < pageSize)
            {
                return null;
            }

            return new Link(path, position);
        }
    }

    class Embedded
    {
        public List<object> Page { get; set; }
    }

    public static class Direction
    {
        public static int Forwards => 1;
        public static int Backwards => -1;
    }
}