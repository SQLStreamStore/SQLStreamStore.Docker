namespace SqlStreamStore.HAL.DevServer
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using KestrelPureOwin;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.Owin;
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
        private const int DefaultPort = 8001;

        public static async Task<int> Main(string[] args)
        {
            var options = new KestrelServerOptions
            {
                AddServerHeader = false
            };

            if(!int.TryParse(args.FirstOrDefault(), out var port))
            {
                port = DefaultPort;
            };

            using(var server = new KestrelOwinServer(options))
            using(var streamStore = new InMemoryStreamStore())
            {
                var url = new UriBuilder { Port = port }.Uri.ToString();
                var owinPipeline = Configure(streamStore);

                await server.Start(url, owinPipeline, CancellationToken.None);

                DisplayMenu(streamStore, url);
            }

            return 0;
        }

        private static Action<BuildFunc> Configure(IStreamStore streamStore)
            => builder => builder
                .Use(CatchAndDisplayErrors)
                .Use(AllowAllOrigins)
                .Use(SqlStreamStoreHalMiddleware.UseSqlStreamStoreHal(streamStore));

        private static MidFunc CatchAndDisplayErrors => next => async env =>
        {
            try
            {
                await next(env);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);    
            }
        };

        // don't actually do this in production
        private static MidFunc AllowAllOrigins => next => env =>
        {
            var context = new OwinContext(env);
            
            context.Response.OnSendingHeaders(_ =>
            {
                var response = (IOwinResponse) _;
                response.Headers["Access-Control-Allow-Origin"] = "*";
            }, context.Response);
            
            return next(env);
        };

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
                    $@"{{ ""foo"": ""{Guid.NewGuid()}"", ""baz"": {{  }}, ""qux"": [ {string.Join(", ", Enumerable
                        .Range(0, messageCount).Select(max => s_random.Next(max)))} ] }}",
                    "{}"))
                .ToArray();
        }
    }
}