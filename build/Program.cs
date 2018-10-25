using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Bullseye.Targets;
using static SimpleExec.Command;

static class Program
{
    private const string ArtifactsDir = "artifacts";
    private const string PublishDir = "publish";

    private const string Clean = nameof(Clean);
    private const string GenerateDocumentation = nameof(GenerateDocumentation);
    private const string Build = nameof(Build);
    private const string RunTests = nameof(RunTests);
    private const string Pack = nameof(Pack);
    private const string Publish = nameof(Publish);
    private const string Push = nameof(Push);

    public static void Main(string[] args)
    {
        var buildNumber = GetBuildNumber();
        var branch = GetBranch();
        var commitHash = GetCommitHash();
        var buildMetadata = $"{branch}.{commitHash}";
        var apiKey = Environment.GetEnvironmentVariable("MYGET_API_KEY");

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
            GenerateDocumentation,
            () =>
            {
                var srcDirectory = new DirectoryInfo("./src");

                var schemaFiles = srcDirectory.GetFiles("*.schema.json", SearchOption.AllDirectories);

                var schemaDirectories = schemaFiles
                    .Select(schemaFile => schemaFile.DirectoryName)
                    .Distinct()
                    .Select(schemaDirectory =>
                        schemaDirectory.Replace(Path.DirectorySeparatorChar,
                            '/')); // normalize paths; yarn/node can handle forward slashes

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Run("cmd", "/c yarn", "docs");
                }
                else
                {
                    Run("yarn", string.Empty, "docs");
                }

                foreach (var schemaDirectory in schemaDirectories)
                {
                    Run(
                        "node",
                        $"node_modules/@adobe/jsonschema2md/cli.js -n --input {schemaDirectory} --out {schemaDirectory} --schema-out=-",
                        "docs");
                }
            });

        Target(
            Build,
            DependsOn(GenerateDocumentation),
            () => Run(
                "dotnet",
                $"build src/SqlStreamStore.HAL.sln -c Release /p:BuildNumber={buildNumber} /p:BuildMetadata={buildMetadata}"));

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
                $"publish --configuration=Release --output=../../{PublishDir} --runtime=alpine.3.7-x64 /p:ShowLinkerSizeComparison=true /p:BuildNumber={buildNumber} /p:BuildMetadata={buildMetadata} src/SqlStreamStore.HAL.DevServer "));
        
        Target(
            Pack,
            DependsOn(Publish),
            () => Run(
                "dotnet",
                $"pack src/SqlStreamStore.HAL -c Release -o ../../{ArtifactsDir} /p:BuildNumber={buildNumber} /p:BuildMetadata={buildMetadata} --no-build"));

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
        => (Environment.GetEnvironmentVariable("TRAVIS_BUILD_NUMBER") ?? "0");
}