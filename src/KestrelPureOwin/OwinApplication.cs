using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.ObjectPool;
using static KestrelPureOwin.Constants;

namespace KestrelPureOwin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    internal class OwinApplication : IHttpApplication<Dictionary<string, object>>
    {
        private static readonly ObjectPoolProvider PoolProvider = new DefaultObjectPoolProvider();

        public OwinApplication(AppFunc application)
        {
            Application = application;
            EnvironmentPool = PoolProvider.Create<Dictionary<string, object>>();
        }

        private AppFunc Application { get; }

        private ObjectPool<Dictionary<string, object>> EnvironmentPool { get; }

        public Dictionary<string, object> CreateContext(IFeatureCollection features)
        {
            var environment = EnvironmentPool.Get();

            var request = features.Get<IHttpRequestFeature>();
            var response = features.Get<IHttpResponseFeature>();
            var connection = features.Get<IHttpConnectionFeature>();
            var lifetime = features.Get<IHttpRequestLifetimeFeature>();
            var identifier = features.Get<IHttpRequestIdentifierFeature>();
            var authentication = features.Get<IHttpAuthenticationFeature>();

            environment.Clear();

            environment.Set(Owin.Request.Body, request.Body);
            environment.Set(Owin.Request.Headers, new OwinHeaderDictionary(request.Headers));
            environment.Set(Owin.Request.Method, request.Method);
            environment.Set(Owin.Request.Path, request.Path);
            environment.Set(Owin.Request.PathBase, request.PathBase);
            environment.Set(Owin.Request.Protocol, request.Protocol);
            environment.Set(Owin.Request.QueryString, request.QueryString.TrimStart('?'));
            environment.Set(Owin.Request.Scheme, request.Scheme);
            environment.Set(Owin.Request.User, authentication?.User);
            environment.Set(Owin.Request.Id, identifier?.TraceIdentifier);

            environment.Set(Owin.Response.Body, response.Body);
            environment.Set(Owin.Response.Headers, new OwinHeaderDictionary(response.Headers));
            environment.Set(Owin.Response.StatusCode, response.StatusCode);
            environment.Set(Owin.Response.ReasonPhrase, response.ReasonPhrase);
            environment.Set(Owin.Response.Protocol, request.Protocol);

            environment.Set(Owin.CallCancelled, lifetime?.RequestAborted ?? CancellationToken.None);
            environment.Set(Owin.OwinVersion, "1.1");

            environment.Set(Server.OnSendingHeaders, CreateCallback(response.OnStarting));
            environment.Set(Server.RemoteIpAddress, connection?.RemoteIpAddress.ToString());
            environment.Set(Server.RemotePort, connection?.RemotePort.ToString());
            environment.Set(Server.LocalIpAddress, connection?.LocalIpAddress.ToString());
            environment.Set(Server.LocalPort, connection?.LocalPort.ToString());
            environment.Set(Server.User, authentication?.User);
            environment.Set(Server.Features, features);

            response.OnStarting(SetStatus, (response, environment));

            return environment;
        }

        private static Task SetStatus(object state)
        {
            var (response, environment) = ((IHttpResponseFeature, Dictionary<string, object>)) state;

            response.StatusCode = environment.Get<int>(Owin.Response.StatusCode);
            response.ReasonPhrase = environment.Get<string>(Owin.Response.ReasonPhrase);

            return Tasks.Completed;
        }

        public async Task ProcessRequestAsync(Dictionary<string, object> environment)
        {
            await Application.Invoke(environment).ConfigureAwait(false);

            var features = environment.Get<IFeatureCollection>(Server.Features);

            var response = features.Get<IHttpResponseFeature>();

            var owinHeaders = environment.Get<IDictionary<string, string[]>>(Owin.Response.Headers);

            response.Body = environment.Get<Stream>(Owin.Response.Body);
            response.Headers = new ReverseOwinHeaderDictionary(owinHeaders);
        }

        public void DisposeContext(Dictionary<string, object> environment, Exception exception)
        {
            EnvironmentPool.Return(environment);
        }

        private static Action<Action<object>, object> CreateCallback(Action<Func<object, Task>, object> onStarting)
        {
            return (callback, state) => onStarting(WrapCallback(callback), state);
        }

        private static Func<object, Task> WrapCallback(Action<object> callback)
        {
            return state =>
            {
                callback(state);
                return Tasks.Completed;
            };
        }
    }
}
