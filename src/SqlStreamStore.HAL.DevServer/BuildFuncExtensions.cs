namespace SqlStreamStore.HAL.DevServer
{
    using MidFunc = System.Func<
        System.Func<
            System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task>,
        System.Func<
            System.Collections.Generic.IDictionary<string, object>,
            System.Threading.Tasks.Task>>;

    using BuildFunc = System.Action<
        System.Func<
            System.Func<
                System.Collections.Generic.IDictionary<string, object>,
                System.Threading.Tasks.Task>,
            System.Func<
                System.Collections.Generic.IDictionary<string, object>,
                System.Threading.Tasks.Task>>>;

    public static class BuildFuncExtensions
    {
        public static BuildFunc Use(this BuildFunc builder, MidFunc middleware)
        {
            builder(middleware);
            return builder;
        }
    }
}