namespace SqlStreamStore.HAL.Demo
{
    using System;
    using System.Collections.Generic;
    using SqlStreamStore.Streams;
    using StreamStoreStore.Json;

    public static class SeedData
    {
        static readonly Dictionary<int, Func<object>> Factory = new Dictionary<int, Func<object>>
        {
            { 0, Fizz.Get },
            { 1, FizzBuzz.Get },
            { 2, Buzz.Get }
        };

        public static IEnumerable<NewStreamMessage> Get(int count)
        {
            var rand = new Random();

            for(var i = 0; i < count; i++)
            {
                var random = rand.Next(0, 3);
                var message = Factory[random]();

                yield return new NewStreamMessage(
                    Guid.NewGuid(),
                    message.GetType().FullName,
                    SimpleJson.SerializeObject(message)
                );
            }
        }
    }

    public class Fizz
    {
        public int Lorem { get; set; }

        public int Ipsum { get; set; }

        public int Dolor { get; set; }

        public static Fizz Get()
        {
            var rand = new Random();
            return new Fizz
            {
                Lorem = rand.Next(0, 10000000),
                Ipsum = rand.Next(0, 10000),
                Dolor = rand.Next(0, 100)
            };
        }
    }

    public class Buzz
    {
        public int Consectetur { get; set; }

        public int Adipiscing { get; set; }

        public int Aliquam { get; set; }

        public static Buzz Get()
        {
            var rand = new Random();
            return new Buzz
            {
                Consectetur = rand.Next(0, 10000000),
                Adipiscing = rand.Next(0, 10000),
                Aliquam = rand.Next(0, 100)
            };
        }
    }

    public class FizzBuzz
    {
        public int Hendrerit { get; set; }

        public int Efficitur { get; set; }

        public int Libero { get; set; }

        public static FizzBuzz Get()
        {
            var rand = new Random();
            return new FizzBuzz
            {
                Hendrerit = rand.Next(0, 10000000),
                Efficitur = rand.Next(0, 10000),
                Libero = rand.Next(0, 100)
            };
        }
    }
}