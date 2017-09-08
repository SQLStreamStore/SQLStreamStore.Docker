using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static KestrelPureOwin.Constants;

namespace KestrelPureOwin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public static class OwinServerFactory
    {
        public static void Initialize(IDictionary<string, object> properties)
        {
            properties.Set(Owin.OwinVersion, "1.1");

            // TODO: Add capabilities.
        }

        public static IDisposable Create(AppFunc app, IDictionary<string, object> properties)
        {
            return new KestrelOwinServer(properties);
        }
    }
}