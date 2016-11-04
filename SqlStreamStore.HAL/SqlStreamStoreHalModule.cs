using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nancy;
using SqlStreamStore.Streams;

namespace SqlStreamStore.HAL
{
    public class SqlStreamStoreHalModule : NancyModule
    {
        readonly Dictionary<string, int> _directionLookup = new Dictionary<string, int>
        {
            { "forwards", Direction.Forwards },
            { "backwards", Direction.Backwards }
        };

        public SqlStreamStoreHalModule(SqlStreamStoreHalSettings settings)
            : base(settings.BaseUrl)
        {
            Get["stream", true] = async (_, ct) =>
            {
                var direction = GetDirection();
                var position = GetPosition();

                var readAllPage = await GetReadAllPage(settings.Store, settings.PageSize, position, direction);
                return Response.AsJson(HalResponse.GetPage(readAllPage.Messages, settings.PageSize, Request.Path, direction));
            };

            Get["stream/{position}", true] = async (arg, ct) =>
            {
                var message = await settings.Store.ReadAllForwards(arg.Position, 1);
                return FormatterExtensions.AsJson(Response, HalResponse.GetMessage(message.Messages[0]));
            };
        }

        private long GetPosition()
        {
            long position;

            if (!long.TryParse((string)Request.Query.Position ?? "0", out position))
            {
                throw new Exception("position must be a long");
            }

            return position;
        }

        private int GetDirection()
        {
            string direction = Request.Query.Direction;

            if (!_directionLookup.ContainsKey(direction))
            {
                throw new Exception("direction parameter must be {forwards} or {backwards}");
            }

            return _directionLookup[direction];
        }

        private Task<ReadAllPage> GetReadAllPage(IReadonlyStreamStore store, int pageSize, long? position, int direction)
        {
            return direction == Direction.Forwards ?
                store.ReadAllForwards(position ?? Position.Start, pageSize) :
                store.ReadAllBackwards(position ?? Position.End, pageSize);
        }

        static class Direction
        {
            public static int Forwards => 1;
            public static int Backwards => -1;
        }
    }
}