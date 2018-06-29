namespace SqlStreamStore.HAL.DevServer
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using SqlStreamStore.Streams;

    internal static class Program
    {
        private static readonly Random s_random = new Random();
        private const int DefaultPort = 8001;

        public static async Task<int> Main(string[] args)
        {
            if(!int.TryParse(args.FirstOrDefault(), out var port))
            {
                port = DefaultPort;
            }

            var url = new UriBuilder { Port = port }.Uri.ToString();

            using(var cts = new CancellationTokenSource())
            using(var streamStore = new InMemoryStreamStore())
            using(var host = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseStartup(new DevServerStartup(streamStore))
                .Build())
            {

                var serverTask = host.RunAsync(cts.Token);

                DisplayMenu(streamStore, url);

                await serverTask;
            }

            return 0;
        }

        private static void DisplayMenu(IStreamStore streamStore, string url)
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
    }
}