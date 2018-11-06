using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static Bullseye.Targets;
using static SimpleExec.Command;

static class Program
{
    private const string ArtifactsDir = "artifacts";
    private const string PublishDir = "publish";

    private const string Clean = nameof(Clean);
    private const string Init = nameof(Init);
    private const string GenerateDocumentation = nameof(GenerateDocumentation);
    private const string Build = nameof(Build);
    private const string RunTests = nameof(RunTests);
    private const string Pack = nameof(Pack);
    private const string Publish = nameof(Publish);
    private const string Push = nameof(Push);

    public static void Main(string[] args)
    {
        var apiKey = Environment.GetEnvironmentVariable("MYGET_API_KEY");
        var srcDirectory = new DirectoryInfo("./src");

        Target(Clean, () =>
        {
            if (Directory.Exists(ArtifactsDir))
            {
                Directory.Delete(ArtifactsDir, true);
            }

            if (Directory.Exists(PublishDir))
            {
                Directory.Delete(PublishDir, true);
            }
        });

        Target(
            Init,
            () =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Run("cmd", "/c yarn", "docs");
                }
                else
                {
                    Run("yarn", string.Empty, "docs");
                }
            });

        Target(
            GenerateDocumentation,
            DependsOn(Init),
            ForEach(SchemaDirectories(srcDirectory)),
            schemaDirectory =>
                RunAsync(
                    "node",
                    $"node_modules/@adobe/jsonschema2md/cli.js -n --input {schemaDirectory} --out {schemaDirectory} --schema-out=-",
                    "docs"));

        Target(
            Build,
            DependsOn(GenerateDocumentation),
            () => Run(
                "dotnet",
                "build src/SqlStreamStore.HAL.sln -c Release"));

        Target(
            RunTests,
            DependsOn(Build),
            () => Run(
                "dotnet",
                $"test src/SqlStreamStore.HAL.Tests -c Release -r ../../{ArtifactsDir} --verbosity normal --no-build -l trx;LogFileName=SqlStreamStore.HAL.Tests.xml"));

        Target(
            Publish,
            DependsOn(Build),
            () => Run(
                "dotnet",
                $"publish --configuration=Release --output=../../{PublishDir} --runtime=alpine.3.7-x64 /p:ShowLinkerSizeComparison=true src/SqlStreamStore.HAL.DevServer"));

        Target(
            Pack,
            DependsOn(Publish),
            () => Run(
                "dotnet",
                $"pack src/SqlStreamStore.HAL -c Release -o ../../{ArtifactsDir} --no-build"));

        Target(
            Push,
            DependsOn(Pack),
            () =>
            {
                var packagesToPush = Directory.GetFiles(ArtifactsDir, "*.nupkg", SearchOption.TopDirectoryOnly);
                Console.WriteLine($"Found packages to publish: {string.Join("; ", packagesToPush)}");

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Console.WriteLine("MyGet API key not available. Packages will not be pushed.");
                    return;
                }

                foreach (var packageToPush in packagesToPush)
                {
                    Run(
                        "dotnet",
                        $"nuget push {packageToPush} -s https://www.myget.org/F/sqlstreamstore/api/v3/index.json -k {apiKey}");
                }
            });

        Target("default", DependsOn(Clean, RunTests, Push));

        RunTargets(args.Concat(new[] {"--parallel"}));
    }

    private static string[] SchemaDirectories(DirectoryInfo srcDirectory)
        => srcDirectory.GetFiles("*.schema.json", SearchOption.AllDirectories)
            .Select(schemaFile => schemaFile.DirectoryName)
            .Distinct()
            .Select(schemaDirectory => schemaDirectory.Replace(Path.DirectorySeparatorChar, '/'))
            .ToArray();
}