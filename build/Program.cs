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

    public static void Main(string[] args)
    {
        const string clean = nameof(Clean);
        const string build = nameof(Build);
        const string publish = nameof(Publish);

        Target(
            clean,
            Clean);

        Target(
            build,
            DependsOn(clean),
            Build);

        Target(
            publish,
            DependsOn(build),
            Publish);

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

    private static readonly Action Build = () => Run(
        "dotnet",
        "build SqlStreamStore.Server.sln --configuration Release");

    private static readonly Action Publish = () => Run(
        "dotnet",
        $"publish --configuration=Release --output=../../{PublishDir} --runtime=alpine-x64 /p:ShowLinkerSizeComparison=true src/SqlStreamStore.Server");
}