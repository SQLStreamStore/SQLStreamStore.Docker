using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;
using SqlStreamStore.HAL;

namespace SqlStreamStore.Server
{
    internal class Program : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly SqlStreamStoreServerConfiguration _configuration;
        private readonly SqlStreamStoreFactory _factory;

        public static async Task<int> Main(string[] args)
        {
            var configuration = new SqlStreamStoreServerConfiguration(
                Environment.GetEnvironmentVariables(),
                args);

            using (var program = new Program(configuration))
            {
                return await program.Run();
            }
        }

        private Program(SqlStreamStoreServerConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(configuration.LogLevel)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information(configuration.ToString());

            _configuration = configuration;
            _cts = new CancellationTokenSource();
            _factory = new SqlStreamStoreFactory(configuration);
        }

        private async Task<int> Run()
        {
            try
            {
                switch (_configuration.Args.FirstOrDefault())
                {
                    case "initialize":
                    case "init":
                        await RunInitialization();
                        return 0;
                    case "initialize-database":
                    case "init-database":
                        await RunDatabaseInitialization();
                        return 0;
                    default:
                        await RunServer();
                        return 0;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly.");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private async Task RunServer()
        {
            using (var streamStore = _factory.Create())
            using (var host = new WebHostBuilder()
                .SuppressStatusMessages(true)
                .UseKestrel()
                .UseStartup(new SqlStreamStoreServerStartup(
                    streamStore,
                    new SqlStreamStoreMiddlewareOptions
                    {
                        UseCanonicalUrls = _configuration.UseCanonicalUris,
                        ServerAssembly = typeof(Program).Assembly
                    }))
                .UseSerilog()
                .Build())
            {
                await Task.WhenAll(
                    host.RunAsync(_cts.Token),
                    host.WaitForShutdownAsync(_cts.Token));
            }
        }

        private Task RunInitialization()
            => new SqlStreamStoreInitializer(_configuration).Initialize(_cts.Token);

        private Task RunDatabaseInitialization()
            => new DatabaseInitializer(_configuration).Initialize(_cts.Token);

        public void Dispose()
        {
            _cts?.Dispose();
        }
    }
}