using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;
using SqlStreamStore.HAL;
using static SqlStreamStore.Server.Constants;

namespace SqlStreamStore.Server
{
    internal class SqlStreamStoreServer : IDisposable
    {
        private static readonly ILogger s_Log = Log.ForContext<SqlStreamStoreServer>();

        private readonly CancellationTokenSource _cts;
        private readonly SqlStreamStoreServerConfiguration _configuration;
        private readonly SqlStreamStoreFactory _factory;

        public static async Task<int> Main(string[] args)
        {
            var configuration = new SqlStreamStoreServerConfiguration(
                Environment.GetEnvironmentVariables(),
                args);

            using (var server = new SqlStreamStoreServer(configuration))
            {
                return await server.Run();
            }
        }

        private SqlStreamStoreServer(SqlStreamStoreServerConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(configuration.LogLevel)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            s_Log.Information(configuration.ToString());

            switch (configuration.Provider)
            {
                case inmemory:
                    if (configuration.ConnectionString != default)
                    {
                        ConfigurationNotSupported(inmemory, nameof(_configuration.ConnectionString));
                    }

                    if (configuration.Schema != default)
                    {
                        ConfigurationNotSupported(inmemory, nameof(_configuration.Schema));
                    }

                    if (configuration.DisableDeletionTracking)
                    {
                        ConfigurationNotSupported(inmemory, nameof(_configuration.DisableDeletionTracking));
                    }

                    break;
                case mysql:
                    if (configuration.Schema != default)
                    {
                        ConfigurationNotSupported(mysql, nameof(_configuration.Schema));
                    }

                    break;
            }

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
                s_Log.Fatal(ex, "Host terminated unexpectedly.");
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
                        ServerAssembly = typeof(SqlStreamStoreServer).Assembly
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

        private static void ConfigurationNotSupported(string provider, string configurationKey) =>
            s_Log.Warning(
                "Configuration key '{configurationKey}' is not supported for provider {provider}. It will be ignored.",
                configurationKey,
                provider);

        public void Dispose()
        {
            _cts?.Dispose();
        }
    }
}