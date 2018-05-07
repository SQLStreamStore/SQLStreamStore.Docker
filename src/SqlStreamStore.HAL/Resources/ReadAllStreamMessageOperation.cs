namespace SqlStreamStore.HAL.Resources
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using SqlStreamStore.Streams;

    internal class ReadAllStreamMessageOperation : IStreamStoreOperation<StreamMessage>
    {
        public ReadAllStreamMessageOperation(IOwinRequest request)
        {
            Position = long.Parse(request.Path.Value.Remove(0, 1));
        }

        public long Position { get; }

        public async Task<StreamMessage> Invoke(IStreamStore streamStore, CancellationToken ct)
        {
            var page = await streamStore.ReadAllForwards(Position, 1, true, ct);

            return page.Messages.Where(m => m.Position == Position).FirstOrDefault();
        }
    }
}