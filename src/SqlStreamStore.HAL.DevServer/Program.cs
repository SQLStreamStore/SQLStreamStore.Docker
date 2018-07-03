namespace SqlStreamStore.HAL.DevServer
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;

    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            using(var cts = new CancellationTokenSource())
            using(var streamStore = new InMemoryStreamStore())
            using(var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup(new DevServerStartup(streamStore))
                .Build())
            {
                await host.RunAsync(cts.Token);
            }

            return 0;
        }
    }
}