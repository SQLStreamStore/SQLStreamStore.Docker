using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static KestrelPureOwin.Constants;

namespace KestrelPureOwin
{
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv;
    using AppFunc = Func<IDictionary<string, object>, Task>;

    using MidFunc = Func<
        Func<IDictionary<string, object>, Task>,
        Func<IDictionary<string, object>, Task>>;

    using BuildFunc = Action<Func<
        Func<IDictionary<string, object>, Task>,
        Func<IDictionary<string, object>, Task>>>;

    public class KestrelOwinServer : IDisposable
    {
        public KestrelOwinServer() : this(new Dictionary<string, object>())
        {
        }

        public KestrelOwinServer(IDictionary<string, object> properties) : this(GetOptions(properties))
        {
            var addresses = properties
                .Get<IList<IDictionary<string, object>>>(Host.Addresses)
                    ?? new List<IDictionary<string, object>>();

            foreach (var address in addresses)
            {
                
            }
        }

        public KestrelOwinServer(KestrelServerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var wrappedOptions = Options.Create(options);
            
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new ConsoleLogProvider());

            var transport = new LibuvTransportFactory(
                Options.Create(new LibuvTransportOptions()),
                new ApplicationLifetime(loggerFactory.CreateLogger<ApplicationLifetime>()),
                loggerFactory);

            Server = new KestrelServer(wrappedOptions, transport, loggerFactory);
        }

        private static KestrelServerOptions GetOptions(IDictionary<string, object> properties)
            => new KestrelServerOptions();

        public KestrelServer Server { get; }

        public Task Start(string url, Action<BuildFunc> configure, CancellationToken cancellationToken)
        {
            var application = ConfigureApplication(configure);

            var addresses = Server.Features.Get<IServerAddressesFeature>();

            if (addresses == null)
            {
                Server.Features.Set(addresses = new ServerAddressesFeature());
            }

            addresses.Addresses.Add(url);

            foreach (var address in addresses.Addresses)
            {
                Console.WriteLine($"Now listening on: {address}");
            }

            return Server.StartAsync(application, cancellationToken);
        }

        public void Dispose() => Server.Dispose();

        private static OwinApplication ConfigureApplication(Action<BuildFunc> configure)
        {
            var middleware = new List<MidFunc>();

            var builder = new BuildFunc(middleware.Add);

            configure(builder);

            return BuildApplication(middleware);
        }

        private static OwinApplication BuildApplication(IEnumerable<MidFunc> middleware)
        {
            var end = new AppFunc(env => Tasks.Completed);

            var app = middleware.Reverse().Aggregate(end, (current, next) => next(current));

            return new OwinApplication(app);
        }

        private class ConsoleLogger : ILogger
        {
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) 
                => Console.WriteLine(formatter(state, exception));

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) => new NoOpScope();
            
            private class NoOpScope : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }

        private class ConsoleLogProvider : ILoggerProvider
        {
            public void Dispose()
            {
                
            }

            public ILogger CreateLogger(string categoryName) => new ConsoleLogger();
        }
    }
}