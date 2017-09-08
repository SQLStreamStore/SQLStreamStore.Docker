using System.Threading.Tasks;

namespace KestrelPureOwin
{
    internal static class Tasks
    {
#if NET451
        public static readonly Task Completed = Task.FromResult(0);
#else
        public static readonly Task Completed = Task.CompletedTask;
#endif
    }
}