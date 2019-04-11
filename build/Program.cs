using System;
using System.IO;
using System.Linq;
using Fclp;
using static Bullseye.Targets;
using static SimpleExec.Command;

static class Program
{
    private const string ArtifactsDir = "artifacts";
    private const string PublishDir = "publish";

    public static void Main(string[] args)
    {
        const string clean = nameof(clean);
        const string build = nameof(build);
        const string test = nameof(test);
        const string publish = nameof(publish);

        var runtime = "alpine-x64";
        var libraryVersion = "1.2.0-beta.*";

        var parser = new FluentCommandLineParser();
        parser.Setup<string>("runtime")
            .Callback(r => runtime = r);
        parser.Setup<string>("library-version")
            .Callback(v => libraryVersion = v);

        var result = parser.Parse(args);

        args = result
            .AdditionalOptions
            .SelectMany(option => new[] {option.Key.Length == 1 ? $"-{option.Key}" : $"--{option.Key}", option.Value})
            .Where(arg => arg != null)
            .ToArray();

        Target(
            clean,
            Clean);

        Target(
            build,
            DependsOn(clean),
            Build(libraryVersion));

        Target(
            test,
            DependsOn(build),
            Test);

        Target(
            publish,
            DependsOn(test),
            Publish(runtime, libraryVersion));

        Target("default", DependsOn(publish));

        RunTargetsAndExit(args);
    }

    private static readonly Action Clean = () =>
    {
        if (Directory.Exists(ArtifactsDir))
        {
            Directory.Delete(ArtifactsDir, true);
        }

        if (Directory.Exists(PublishDir))
        {
            Directory.Delete(PublishDir, true);
        }
    };

    private static Action Build(string libraryVersion) => () => Run(
        "dotnet",
        $"build SqlStreamStore.Server.sln --configuration=Release /p:LibraryVersion={libraryVersion}");

    private static readonly Action Test = () => Run(
        "dotnet",
        $"test --configuration=Release --no-build");

    private static Action Publish(string runtime, string libraryVersion) => () => Run(
        "dotnet",
        $"publish --configuration=Release --output=../../{PublishDir} --runtime={runtime} /p:ShowLinkerSizeComparison=true /p:LibraryVersion={libraryVersion} src/SqlStreamStore.Server");
}