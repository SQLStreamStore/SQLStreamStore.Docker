#addin "Cake.FileHelpers"
#addin "Cake.Powershell"

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
        EnvironmentVariables = DotNetEnvironment
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

    foreach(var testProject in testProjects) {
        var projectDir = srcDir + Directory(testProject);
        StartProcess("dotnet", new ProcessSettings {
            Arguments = "xunit",
            WorkingDirectory = projectDir,
            EnvironmentVariables = DotNetEnvironment
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
        VersionSuffix = versionSuffix,
        EnvironmentVariables = DotNetEnvironment
    });
});

Task("Default")
    .IsDependentOn("RunTests")
    .IsDependentOn("NuGetPack");

RunTarget(target);

Dictionary<string, string> DotNetEnvironment => new Dictionary<string, string> {
    {"PATH", Path}
};

private string _path;

string Path => _path ?? (_path = DownloadDotNetCoreIfNecessary());

string DownloadDotNetCoreIfNecessary() {
    var path = Context.Environment.GetEnvironmentVariable("PATH");
    var version = "2.0.0";

    if (DotNetVersion == System.Version.Parse(version)) {
        return path;
    }

    var dotnetDirectory = Directory(".dotnet");

    EnsureDirectoryExists(dotnetDirectory);

    if (IsRunningOnWindows()) {
        DownloadDotNetCoreForWindows(dotnetDirectory, version);
    } else {
        DownloadDotNetCoreForUnix(dotnetDirectory, version);
    }
    return $"{dotnetDirectory.Path.MakeAbsolute(Context.Environment)};{path}";
}

void DownloadDotNetCoreForWindows(ConvertableDirectoryPath dotnetDirectory, string version) {
    var channel = "Current";
    var installer = "https://dot.net/dotnet-install.ps1";
    var installerPath = dotnetDirectory + File("dotnet-install.ps1");

    Information(installerPath);

    DownloadFile(installer, installerPath);

    StartPowershellFile(installerPath, args => {
        args.Append("Channel", channel);
        args.Append("Version", version);
        args.Append("InstallDir", dotnetDirectory);
    });
}

void DownloadDotNetCoreForUnix(DirectoryPath dotnetDirectory, string version) {
    var channel = "Current";
    var installer = "https://dot.net/dotnet-install.sh";
    var installerPath = dotnetDirectory + File("dotnet-install.sh");

    DownloadFile(installer, installerPath);

    using (var process = StartAndReturnProcess(installerPath, new ProcessSettings {
        Arguments = $"--channel {channel} --version {version} --install-dir {dotnetDirectory}"
    })) {
        process.WaitForExit();
    }
}

Version DotNetVersion {
    get {
        using (var process = StartAndReturnProcess("dotnet", new ProcessSettings {
            Arguments = "--version",
            RedirectStandardOutput = true
        })) {
            process.WaitForExit();

            var stdout = process.GetStandardOutput().FirstOrDefault();

            System.Version installedVersion;

            System.Version.TryParse(stdout, out installedVersion);

            return installedVersion;
        }
    }
}
