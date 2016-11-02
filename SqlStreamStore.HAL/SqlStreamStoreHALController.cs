using System.Collections.Generic;
using System.Web.Http;
using SqlStreamStore.Streams;

namespace SqlStreamStore.HAL
{
    public class SqlStreamStoreHalController : ApiController
    {
        readonly Dictionary<string, int> _directionLookup = new Dictionary<string, int>
        {
            { "forwards", Direction.Forwards },
            { "backwards", Direction.Backwards }

        };

        readonly IReadonlyStreamStore _store;

        readonly int _pageSize;

        public SqlStreamStoreHalController(HALSettings settings)
        {
            _store = settings.Store;
            _pageSize = settings.PageSize;
        }

        [HttpGet]
        public IHttpActionResult Index(string direction, long? position = null)
        {
            var dir = _directionLookup[direction];
            var readAllPage = GetStream(position, dir);
            var response = HalResponse.GetPage(readAllPage.Messages, _pageSize, Request.RequestUri.AbsolutePath, dir);

            return Ok(response);
        }

        [HttpGet]
        public IHttpActionResult Message(long position)
        {
            var message = _store.ReadAllForwards(position, 1).GetAwaiter().GetResult();
            dynamic response = HalResponse.GetMessage(message.Messages[0]);
            
            return Ok(response);
        }

        private ReadAllPage GetStream(long? position, int direction)
        {
            return direction == Direction.Forwards ? 
                _store.ReadAllForwards(position ?? Position.Start, _pageSize).GetAwaiter().GetResult() : 
                _store.ReadAllBackwards(position ?? Position.End, _pageSize).GetAwaiter().GetResult();
        }

        static class Direction
        {
            public static int Forwards => 1;
            public static int Backwards => -1;
        }
    }
}