namespace SqlStreamStore.HAL.Resources
{
    using System.Threading;
    using System.Threading.Tasks;

    internal interface IStreamStoreOperation<T>
    {
        Task<T> Invoke(IStreamStore streamStore, CancellationToken cancellationToken);
    }
}