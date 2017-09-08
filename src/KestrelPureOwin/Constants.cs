namespace KestrelPureOwin
{
    internal static class Constants
    {
        public static class Owin
        {
            public const string CallCancelled = "owin.CallCancelled";
            public const string OwinVersion = "owin.Version";

            public static class Request
            {
                public const string Body = "owin.RequestBody";
                public const string Headers = "owin.RequestHeaders";
                public const string Method = "owin.RequestMethod";
                public const string Path = "owin.RequestPath";
                public const string PathBase = "owin.RequestPathBase";
                public const string Protocol = "owin.RequestProtocol";
                public const string QueryString = "owin.RequestQueryString";
                public const string Scheme = "owin.RequestScheme";
                public const string User = "owin.RequestUser";
                public const string Id = "owin.RequestId";
            }

            public static class Response
            {
                public const string Body = "owin.ResponseBody";
                public const string Headers = "owin.ResponseHeaders";
                public const string StatusCode = "owin.ResponseStatusCode";
                public const string ReasonPhrase = "owin.ResponseReasonPhrase";
                public const string Protocol = "owin.ResponseProtocol";
            }
        }

        public static class Server
        {
            public const string RemoteIpAddress = "server.RemoteIpAddress";
            public const string RemotePort = "server.RemotePort";
            public const string LocalIpAddress = "server.LocalIpAddress";
            public const string LocalPort = "server.LocalPort";
            public const string User = "server.User";
            public const string Features = "server.Features";
            public const string Capabilities = "server.Capabilities";
            public const string Name = "server.Name";
            public const string OnSendingHeaders = "server.OnSendingHeaders";
        }

        public static class Host
        {
            public const string Addresses = "host.Addresses";
        }
    }
}
