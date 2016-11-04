using Microsoft.Owin.Hosting;
using SqlStreamStore.Streams;
using System;
using System.Linq;

namespace SqlStreamStore.HAL
{
    class Program
    {
        static void Main()
        {
            var streamStore = new InMemoryStreamStore();
            var messages = SeedData.Get(40);

            streamStore.AppendToStream("SomeStream", ExpectedVersion.Any, messages).GetAwaiter().GetResult();
            streamStore.AppendToStream("SomeOtherStream", ExpectedVersion.Any, messages).GetAwaiter().GetResult();

            var settings = new SqlStreamStoreHalSettings
            {
                Store = streamStore,
                PageSize = 100
            };

            var baseUrl = "http://localhost:8080";

            using (WebApp.Start(baseUrl, app => app.UseSqlStreamStoreHal(settings)))
            {
                Console.WriteLine("Press Enter to quit.");
                Console.ReadKey();
            }
        }
    }
}
