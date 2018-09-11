using System;
using System.IO;
using static Bullseye.Targets;
using static SimpleExec.Command;

static class Program
{
    private const string ArtifactsDir = "artifacts";

    private const string Clean = nameof(Clean);
    private const string Build = nameof(Build);
    private const string RunTests = nameof(RunTests);
    private const string Pack = nameof(Pack);
    private const string Publish = nameof(Publish);

    public static void Main(string[] args)
    {
        var buildNumber = Environment.GetEnvironmentVariable("TRAVIS_BUILD_NUMBER") ?? "0";
        var versionSuffix = "build" + buildNumber.PadLeft(5, '0');
        var apiKey = Environment.GetEnvironmentVariable("MYGET_API_KEY");

        Target(Clean, () =>
        {
            if (Directory.Exists(ArtifactsDir))
            {
                Directory.Delete(ArtifactsDir, true);
            }
        });

        Target(Build, () => Run("dotnet", "build src/SqlStreamStore.HAL.sln -c Release"));

        Target(
            RunTests,
            DependsOn(Build),
            () => Run(
                "dotnet",
                $"test src/SqlStreamStore.HAL.Tests -c Release -r ../../{ArtifactsDir} --no-build -l trx;LogFileName=SqlStreamStore.HAL.Tests.xml"));

        Target(
            Pack,
            DependsOn(Build),
            () => Run("dotnet",
                $"pack src/SqlStreamStore.HAL -c Release -o ../../{ArtifactsDir} --no-build --version-suffix {versionSuffix}"));

        Target(Publish, DependsOn(Pack), () =>
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
                Run("dotnet",
                    $"nuget push {packageToPush} -s https://www.myget.org/F/sqlstreamstore/api/v3/index.json -k {apiKey}");
            }
        });

        Target("default", DependsOn(Clean, RunTests, Publish));

        RunTargets(args);
    }
}
