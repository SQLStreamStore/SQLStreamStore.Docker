namespace SqlStreamStore.HAL.Demo
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Owin.Builder;
    using Nowin;
    using Serilog;
    using SqlStreamStore.Streams;

    internal class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .ColoredConsole()
                .MinimumLevel.Verbose()
                .CreateLogger();

            var streamStore = new InMemoryStreamStore();

            var builder = new AppBuilder();

            builder.Use(SqlStreamStoreHalMiddleware.UseSqlStreamStoreHal(streamStore));

            var server = ServerBuilder.New()
                .SetEndPoint(new IPEndPoint(IPAddress.Loopback, 8080))
                .SetOwinApp(builder.Build());


            using(streamStore)
            using(server.Build())
            using(server.Start())
            {
                DisplayMenu(streamStore);
            }
        }

        private static void DisplayMenu(IStreamStore streamStore)
        {
            while(true)
            {
                Console.WriteLine("Press w to write 10 messages each to 100 streams");
                Console.WriteLine("Press t to write 100 messages each to 10 streams");
                Console.WriteLine("Press ESC to exit");

                var key = Console.ReadKey();

                switch(key.Key)
                {
                    case ConsoleKey.Escape:
                        return;
                    case ConsoleKey.W:
                        Write(streamStore, 10, 100);
                        break;
                    case ConsoleKey.T:
                        Write(streamStore, 100, 10);
                        break;
                    default:
                        Console.WriteLine("Computer says no");
                        break;
                }
            }
        }

        private static void Write(IStreamStore streamStore, int messageCount, int streamCount)
            => Task.Run(() => Task.WhenAll(
                from streamId in Enumerable.Range(0, streamCount).Select(_ => $"test-{Guid.NewGuid():n}")
                select streamStore.AppendToStream(streamId,
                    ExpectedVersion.NoStream,
                    GenerateMessages(messageCount))));

        private static NewStreamMessage[] GenerateMessages(int messageCount)
            => Enumerable.Range(0, messageCount)
                .Select(_ => new NewStreamMessage(Guid.NewGuid(), "test", "{}", "{}"))
                .ToArray();
    }
}