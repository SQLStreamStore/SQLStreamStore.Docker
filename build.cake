#addin "nuget:?package=Cake.FileHelpers&version=2.0.0"

var target          = Argument("target", "Default");
var configuration   = Argument("configuration", "Release");
var artifactsDir    = Directory("./artifacts");
var srcDir          = Directory("./src");
var solution        = srcDir + File("SqlStreamStore.HAL.sln");
var buildNumber     = string.IsNullOrWhiteSpace(EnvironmentVariable("BUILD_NUMBER")) ? "0" : EnvironmentVariable("BUILD_NUMBER");

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

Task("RunTests")
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

Task("NuGetPack")
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

Task("Default")
    .IsDependentOn("RunTests")
    .IsDependentOn("NuGetPack");

RunTarget(target);

Action RunTest(string testProject) => () => DotNetCoreTest(srcDir + Directory(testProject), new DotNetCoreTestSettings {
    NoBuild = true,
    NoRestore = true
});