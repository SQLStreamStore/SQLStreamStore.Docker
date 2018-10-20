using System;
using System.IO;
using static Bullseye.Targets;
using static SimpleExec.Command;

static class Program
{
    private const string ArtifactsDir = "artifacts";

    private const string Clean = nameof(Clean);
    private const string GenerateDocumentation = nameof(GenerateDocumentation);
    private const string Build = nameof(Build);
    private const string RunTests = nameof(RunTests);
    private const string Pack = nameof(Pack);
    private const string Publish = nameof(Publish);

    public static void Main(string[] args)
    {
        var buildNumber = GetBuildNumber();
        var branch = GetBranch();
        var commitHash = GetCommitHash();
        var buildMetadata = $"build.{buildNumber}.{branch}.{commitHash}";
        var apiKey = Environment.GetEnvironmentVariable("MYGET_API_KEY");

        Target(Clean, () =>
        {
            if (Directory.Exists(ArtifactsDir))
            {
                Directory.Delete(ArtifactsDir, true);
            }
        });
        
        Target(
            GenerateDocumentation,
            () => Run("dotnet", "build docs/docs.csproj --verbosity normal"));

        Target(
            Build, 
            DependsOn(GenerateDocumentation),
            () => Run(
                "dotnet", 
                $"build src/SqlStreamStore.HAL.sln -c Release /p:BuildMetadata={buildMetadata}"));

        Target(
            RunTests,
            DependsOn(Build),
            () => Run(
                "dotnet",
                $"test src/SqlStreamStore.HAL.Tests -c Release -r ../../{ArtifactsDir} --verbosity normal --no-build -l trx;LogFileName=SqlStreamStore.HAL.Tests.xml"));

        Target(
            Pack,
            DependsOn(Build), 
            () => Run(
                "dotnet",
                $"pack src/SqlStreamStore.HAL -c Release -o ../../{ArtifactsDir} --no-build"));

        Target(
            Publish, 
            DependsOn(Pack), 
            () => {
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

        Target("default", DependsOn(Clean, RunTests, Publish));

        RunTargets(args);
    }

    private static string GetBranch()
        => (Environment.GetEnvironmentVariable("TRAVIS_PULL_REQUEST")?.ToLower() == "false"
                ? null
                : $"pr-{Environment.GetEnvironmentVariable("TRAVIS_PULL_REQUEST")}")
            ?? Environment.GetEnvironmentVariable("TRAVIS_BRANCH") 
            ?? "none";

    private static string GetCommitHash() 
        => Environment.GetEnvironmentVariable("TRAVIS_PULL_REQUEST_SHA")
            ?? Environment.GetEnvironmentVariable("TRAVIS_COMMIT")
            ?? "none";

    private static string GetBuildNumber()
        => (Environment.GetEnvironmentVariable("TRAVIS_BUILD_NUMBER") ?? "0").PadLeft(5, '0');
}