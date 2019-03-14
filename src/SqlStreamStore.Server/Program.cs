using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Serilog;
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
                .Enrich.FromLogContext()
                .WriteTo.Console()
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
                using (var streamStore = await _factory.Create(_cts.Token))
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

        public void Dispose()
        {
            _cts?.Dispose();
        }
    }
}