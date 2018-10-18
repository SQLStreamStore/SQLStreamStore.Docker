namespace SqlStreamStore.HAL
{
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;

    internal static class ResourceMethods
    {
        public static HttpMethod[] Discover<TResource>()
            where TResource : IResource
        {
            var httpMethods = typeof(TResource)
                .GetMethods(
                    (BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Select(method => new HttpMethod(method.Name.ToUpperInvariant()))
                .Concat(new[] { HttpMethod.Options })
                .ToList();

            if(httpMethods.Contains(HttpMethod.Get))
            {
                httpMethods.Add(HttpMethod.Head);
            }

            return httpMethods.ToArray();
        }
    }
}