namespace SqlStreamStore.HAL.Resources
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using SqlStreamStore.Streams;

    internal class ReadAllStreamMessageOptions
    {
        public ReadAllStreamMessageOptions(IOwinRequest request)
        {
            Position = long.Parse(request.Path.Value.Remove(0, 1));
        }

        public long Position { get; }

        public Func<IReadonlyStreamStore, CancellationToken, Task<StreamMessage>> GetReadOperation()
            => async (streamStore, ct) =>
            {
                var page = await streamStore.ReadAllForwards(Position, 1, true, ct);

                return page.Messages.Where(m => m.Position == Position).FirstOrDefault();
            };
    }
}