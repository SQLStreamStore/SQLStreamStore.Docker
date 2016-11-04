using System.Collections.Generic;
using System.Threading.Tasks;
using Nancy;
using SqlStreamStore.Streams;

namespace SqlStreamStore.HAL
{
    public class SqlStreamStoreHalModule : NancyModule
    {
        private readonly Dictionary<string, int> _directionLookup = new Dictionary<string, int>
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
            
            Get["stream/{position}", true] = async (args, ct) =>
            {
                var message = await settings.Store.ReadAllForwards(args.Position, 1);
                var model = HalResponse.GetMessage(message.Messages[0]);
                return FormatterExtensions.AsJson(Response, model);
            };

            Get["streams/{streamId}", true] = async (args, ct) =>
            {
                var direction = GetDirection();
                var position = GetPosition();

                ReadStreamPage readAllPage = await GetReadStreamPage(args.StreamId, settings.Store, settings.PageSize, (int?)position, direction);
                return Response.AsJson(HalResponse.GetPage(readAllPage.Messages, settings.PageSize, Request.Path, direction));
            };
        }

        private long? GetPosition()
        {
            string position = Request.Query.Position;

            if (position == null)
            {
                return null;
            }

            long longPosition;
            if (!long.TryParse(position, out longPosition))
            {
                return null;
            }

            return longPosition;
        }

        private int GetDirection()
        {
            var direction = (string)Request.Query.Direction ?? "forwards";
            return !_directionLookup.ContainsKey(direction) ? Direction.Forwards : _directionLookup[direction];
        }

        private static Task<ReadAllPage> GetReadAllPage(IReadonlyStreamStore store, int pageSize, long? position, int direction)
        {
            return direction == Direction.Forwards ?
                store.ReadAllForwards(position ?? Position.Start, pageSize) :
                store.ReadAllBackwards(position ?? Position.End, pageSize);
        }

        private static Task<ReadStreamPage> GetReadStreamPage(string streamId, IReadonlyStreamStore store, int pageSize, int? position, int direction)
        {
            return direction == Direction.Forwards ?
                store.ReadStreamForwards(streamId, position ?? (int)Position.Start, pageSize) :
                store.ReadStreamBackwards(streamId, position ?? (int)Position.End, pageSize);
        }

        private static class Direction
        {
            public static int Forwards => 1;
            public static int Backwards => -1;
        }
    }
}