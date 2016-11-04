using Microsoft.Owin.Hosting;
using SqlStreamStore.Streams;
using System;

namespace SqlStreamStore.HAL
{
    class Program
    {
        static void Main()
        {
            var streamStore = new InMemoryStreamStore();
            var messages = SeedData.Get(1000);

            streamStore.AppendToStream("SomeStream", ExpectedVersion.Any, messages);

            var settings = new SqlStreamStoreHalSettings
            {
                Store = streamStore,
                PageSize = 20
            };

            var baseUrl = "http://+:8080";

            using (WebApp.Start(baseUrl, app => app.UseSqlStreamStoreHal(settings)))
            {
                Console.WriteLine("Press Enter to quit.");
                Console.ReadKey();
            }
        }
    }
}
