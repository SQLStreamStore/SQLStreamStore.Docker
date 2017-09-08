using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static KestrelPureOwin.Constants;

namespace KestrelPureOwin
{
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

            var lifetime = new ApplicationLifetime();

            var loggerFactory = new LoggerFactory();

            Server = new KestrelServer(wrappedOptions, lifetime, loggerFactory);
        }

        private static KestrelServerOptions GetOptions(IDictionary<string, object> properties)
            => new KestrelServerOptions();

        public KestrelServer Server { get; }

        public void Run(string url, Action<BuildFunc> configure)
        {
            var done = new ManualResetEventSlim(false);

            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Application is shutting down...");

                done.Set();

                e.Cancel = true;
            };

            Start(url, configure);

            done.Wait();
        }

        public void Start(string url, Action<BuildFunc> configure)
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

            Server.Start(application);
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
    }
}