namespace SqlStreamStore.HAL.DevServer
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using SqlStreamStore.Streams;

    internal class Program : IDisposable
    {
        private static readonly Random s_random = new Random();
        private readonly CancellationTokenSource _cts;
        private readonly InMemoryStreamStore _streamStore;
        private readonly IWebHost _host;
        private readonly IConfigurationRoot _configuration;

        private bool Interactive => _configuration.GetValue<bool>("interactive");

        public static async Task<int> Main(string[] args)
        {
            using(var program = new Program(args))
            {
                return await program.Run();
            }
        }

        private Program(string[] args)
        {
            _cts = new CancellationTokenSource();
            _streamStore = new InMemoryStreamStore();
            _host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup(new DevServerStartup(_streamStore))
                .Build();
            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
        }

        private async Task<int> Run()
        {
            try
            {
                var serverTask = _host.RunAsync(_cts.Token);

                if(Interactive)
                {
                    DisplayMenu(_streamStore);
                }

                await serverTask;

                return 0;
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

            return 1;
        }

        private static void DisplayMenu(IStreamStore streamStore, string url = null)
        {
            while(true)
            {
                Console.WriteLine("Using stream store: {0}", streamStore.GetType().Name);
                Console.WriteLine("Press w to write 10 messages each to 100 streams");
                Console.WriteLine("Press t to write 100 messages each to 10 streams");
                Console.WriteLine("Press ESC to exit");

                var key = Console.ReadKey();

                switch(key.Key)
                {
                    case ConsoleKey.Escape:
                        return;
                    case ConsoleKey.W:
                        Write(streamStore, url, 10, 100);
                        break;
                    case ConsoleKey.T:
                        Write(streamStore, url, 100, 10);
                        break;
                    default:
                        Console.WriteLine("Computer says no");
                        break;
                }
            }
        }

        private static void Write(IStreamStore streamStore, string url, int messageCount, int streamCount)
        {
            var streams = Enumerable.Range(0, streamCount).Select(_ => $"test-{Guid.NewGuid():n}").ToList();

            var streamIds = string.Join("\n", streams.Select(streamid => $"{url}streams/{streamid}"));

            Console.WriteLine("\nAbout to create the following streams: ");
            Console.WriteLine(streamIds);

            Task.Run(() => Task.WhenAll(
                from streamId in streams
                select streamStore.AppendToStream(streamId,
                    ExpectedVersion.NoStream,
                    GenerateMessages(messageCount))));
        }

        private static NewStreamMessage[] GenerateMessages(int messageCount)
        {
            return Enumerable.Range(0, messageCount)
                .Select(_ => new NewStreamMessage(
                    Guid.NewGuid(),
                    "test",
                    $@"{{ ""foo"": ""{Guid.NewGuid()}"", ""baz"": {{  }}, ""qux"": [ {
                            string.Join(", ",
                                Enumerable
                                    .Range(0, messageCount).Select(max => s_random.Next(max)))
                        } ] }}",
                    "{}"))
                .ToArray();
        }

        public void Dispose()
        {
            _host?.Dispose();
            _streamStore?.Dispose();
            _cts?.Dispose();
        }
    }
}