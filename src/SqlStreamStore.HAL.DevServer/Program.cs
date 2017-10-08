namespace SqlStreamStore.HAL.DevServer
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using KestrelPureOwin;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using SqlStreamStore.Streams;
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

    internal static class Program
    {
        private static readonly Random s_random = new Random();

        public static async Task<int> Main(string[] args)
        {
            var options = new KestrelServerOptions
            {
                AddServerHeader = false
            };

            using(var server = new KestrelOwinServer(options))
            using(var streamStore = new InMemoryStreamStore())
            {
                await server.Start("http://localhost:8001", Configure(streamStore), CancellationToken.None);

                DisplayMenu(streamStore);
            }

            return 0;
        }

        private static Action<BuildFunc> Configure(IStreamStore streamStore)
            => builder => builder
                .Use(DisplayErrors)
                .Use(SqlStreamStoreHalMiddleware.UseSqlStreamStoreHal(streamStore));

        private static MidFunc DisplayErrors => next => env => next(env).ContinueWith(_ =>
            {
                Console.WriteLine(_.Exception);
                
                return Task.CompletedTask;
            },
            TaskContinuationOptions.OnlyOnFaulted);

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
                .Select(_ => new NewStreamMessage(
                    Guid.NewGuid(),
                    "test",
                    $@"{{ ""foo"": ""{Guid.NewGuid()}"", ""baz"": {{  }}, ""qux"": [ {string.Join(", ", Enumerable
                        .Range(0, messageCount).Select(max => s_random.Next(max)))} ] }}",
                    "{}"))
                .ToArray();
    }
}