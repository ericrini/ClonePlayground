using System;
using System.Diagnostics;
using System.IO;
using Force.DeepCloner;
using Itron.Platform.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace ClonePlayground
{
    class Program
    {
        public static void Main(string[] args)
        {
            int.TryParse(args[0], out int iterations);
            int.TryParse(args[1], out int totalLevels);
            int.TryParse(args[2], out int propertiesPerLevel);
            int.TryParse(args[3], out int leafSizeInBytes);

            Console.WriteLine($"Scenaro (Iterations = {iterations}; Total Levels = {totalLevels}; Properties per Level = {propertiesPerLevel}; Leaf Size in Bytes = {leafSizeInBytes})");

            var program = new Program();
            var source = program.CreateDynamicObject(propertiesPerLevel, totalLevels, leafSizeInBytes);
            program.TestNewtonsoftJsonConvert(source, iterations);
            program.TestNewtonsoftJsonStream(source, iterations);
            program.TestNewtonsoftBsonStream(source, iterations);
            program.TestDeepCloner(source, iterations);
            //program.TestFastDeepCloner(source, iterations);
        }

        public void TestNewtonsoftJsonConvert(DynamicObject source, int iterations)
        {
            Benchmark("NewtonsoftJsonConvert", iterations, () =>
            {
                JsonConvert.DeserializeObject<DynamicObject>(JsonConvert.SerializeObject(source));
            });
        }

        public void TestNewtonsoftJsonStream(DynamicObject source, int iterations)
        {
            var serializer = new JsonSerializer();

            Benchmark("NewtonsoftJsonStream", iterations, () =>
            {
                using (var memoryStream = new MemoryStream())
                using (var streamWriter = new StreamWriter(memoryStream))
                using (var textWriter = new JsonTextWriter(streamWriter))
                {
                    serializer.Serialize(textWriter, source);
                    var streamReader = new StreamReader(memoryStream);
                    var textReader = new JsonTextReader(streamReader);
                    serializer.Deserialize<DynamicObject>(textReader);
                }
            });
        }

        public void TestNewtonsoftBsonStream(DynamicObject source, int iterations)
        {
            var serializer = new JsonSerializer();

            Benchmark("NewtonsoftBsonStream", iterations, () =>
            {
                using (var stream = new MemoryStream())
                using (var writer = new BsonDataWriter(stream))
                using (var reader = new BsonDataReader(stream))
                {
                    serializer.Serialize(writer, source);
                    serializer.Deserialize<DynamicObject>(reader);
                }
            });
        }

        public void TestDeepCloner(DynamicObject source, int iterations)
        {
            Benchmark("DeepCloner", iterations, () =>
            {
                source.DeepClone();
            });
        }

        public void TestFastDeepCloner(DynamicObject source, int iterations)
        {
            Benchmark("FastDeepCloner", iterations, () =>
            {
                DynamicObject destination = FastDeepCloner.DeepCloner.Clone(source);
            });
        }

        private void Benchmark(string name, int iterations, Action action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < iterations; i++)
            {
                action();
            }

            stopwatch.Stop();
            Console.WriteLine($"\t{name}: {stopwatch.Elapsed}");
        }

        private DynamicObject CreateDynamicObject(int propertiesPerLevel, int totalLevels, int leafSizeInBytes)
        {
            DynamicObject result = new DynamicObject();

            for (int i = 0; i < propertiesPerLevel; i++)
            {
                string name = $"Property{i}";

                if (totalLevels > 0)
                {
                    result.Add(name, CreateDynamicObject(propertiesPerLevel, totalLevels - 1, leafSizeInBytes));
                }
                else
                {
                    byte[] buffer = new byte[leafSizeInBytes];
                    new Random().NextBytes(buffer);
                    result.Add(name, buffer);
                }
            }

            return result;
        }
    }
}
