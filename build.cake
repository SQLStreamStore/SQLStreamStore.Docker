#tool "nuget:?package=xunit.runner.console&version=2.1.0"

#addin "Cake.FileHelpers"

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
    DotNetCoreRestore(solution);
    NuGetRestore(solution);
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

    foreach(var testProject in testProjects) {
        var projectDir = srcDir + Directory(testProject);
        StartProcess("dotnet", new ProcessSettings {
            Arguments = "xunit",
            WorkingDirectory = projectDir
        });
    }
});

Task("NuGetPack")
    .IsDependentOn("Build")
    .Does(() =>
{
    var versionSuffix = "build" + buildNumber.ToString().PadLeft(5, '0');

    DotNetCorePack(srcDir + Directory("SqlStreamStore.HAL"), new DotNetCorePackSettings {
        ArgumentCustomization = args => args.Append("/p:Version=1.0.0-" + versionSuffix),
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
