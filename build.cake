#addin "nuget:?package=Cake.FileHelpers&version=2.0.0"

var target          = Argument("target", "Default");
var configuration   = Argument("configuration", "Release");
var artifactsDir    = Directory("./artifacts");
var srcDir          = Directory("./src");
var solution        = srcDir + File("SqlStreamStore.HAL.sln");
var buildNumber     = string.IsNullOrWhiteSpace(EnvironmentVariable("BUILD_NUMBER"))
                        ? "0"
                        : EnvironmentVariable("BUILD_NUMBER");
var mygetApiKey     = EnvironmentVariable("MYGET_API_KEY");

Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
});

Task("RestorePackages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore(solution, new DotNetCoreRestoreSettings {
    });
});

Task("Build")
    .IsDependentOn("RestorePackages")
    .Does(() =>
{
    DotNetCoreBuild(solution, new DotNetCoreBuildSettings {
        Configuration = configuration
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testProjects = new string[] { "SqlStreamStore.HAL.Tests" };

    Parallel.Invoke(new ParallelOptions {

    }, Array.ConvertAll(testProjects, RunTest));
});

Task("Publish")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCorePublish(srcDir + Directory("SqlStreamStore.HAL"), new DotNetCorePublishSettings {
        OutputDirectory = artifactsDir,
        Configuration = configuration,
        Framework = "netstandard2.0"
    });
});

Task("Package")
    .IsDependentOn("Build")
    .Does(() =>
{
    var versionSuffix = "build" + buildNumber.ToString().PadLeft(5, '0');

    DotNetCorePack(srcDir + Directory("SqlStreamStore.HAL"), new DotNetCorePackSettings {
        OutputDirectory = artifactsDir,
        NoBuild = true,
        Configuration = configuration,
        VersionSuffix = versionSuffix
    });
});

Task("MyGetPush")
    .IsDependentOn("Package")
    .Does(() =>
{
    if (string.IsNullOrEmpty(mygetApiKey)) {
        Warning("MyGet API key not available. Packages will not be pushed.");
        return;
    }

    var files = GetFiles("artifacts/*.nupkg");
    foreach(var file in files) {
        Information(file);
        DotNetCoreNuGetPush(file.FullPath, new DotNetCoreNuGetPushSettings
        {
            ApiKey = EnvironmentVariable("MYGET_API_KEY"),
            Source = "https://www.myget.org/F/sqlstreamstore/api/v3/index.json"
        });
    }
});

Task("Default")
    .IsDependentOn("Test")
    .IsDependentOn("MyGetPush");

RunTarget(target);

Action RunTest(string testProject) => () => DotNetCoreTest(srcDir + Directory(testProject), new DotNetCoreTestSettings {
    NoBuild = true,
    NoRestore = true,
    Configuration = configuration
});
