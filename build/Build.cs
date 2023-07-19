using NuGet.Versioning;

using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using Nuke.Components;

using Serilog;

using System;

using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
// ReSharper disable VariantPathSeparationHighlighting

[ShutdownDotNetAfterServerBuild]
internal partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Push);

    private const string DevelopBranch = "dev";
    private const string PreviewBranch = "preview";
    private const string MainBranch = "main";

    private string Version;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    private readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Api key to push packages to nuget.org.")]
    [Secret]
    private string NuGetApiKey;

    [Parameter("Api key to push packages to myget.org.")]
    [Secret]
    private string MyGetApiKey;

    [Solution] private readonly Solution Solution;
    [GitRepository] private readonly GitRepository Repository;

    private AbsolutePath SourceDirectory => RootDirectory / "src";
    private AbsolutePath OutputDirectory => RootDirectory / "output";
    private AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    [LatestNuGetVersion("Hsu.Daemon", IncludePrerelease = false)]
    private readonly NuGetVersion NuGetVersion;

    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();
        NuGetApiKey ??= Environment.GetEnvironmentVariable(nameof(NuGetApiKey));
        MyGetApiKey ??= Environment.GetEnvironmentVariable(nameof(MyGetApiKey));
    }

    private Target Initial => _ => _
        .Description("Initial")
        .OnlyWhenStatic(() => IsServerBuild)
        .Executes(() =>
        {
            Log.Debug("{@NuGetVersion}", NuGetVersion);
            Version = $"{GetVersionPrefix()}{GetVersionSuffix()}";
        });

    private Target Clean => _ => _
        .Description("Clean Solution")
        .DependsOn(Initial)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
            OutputDirectory.CreateOrCleanDirectory();
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    private Target Restore => _ => _
        .Description("Restore Solution")
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    private Target Compile => _ => _
        .Description("Compile Solution")
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetVersion(Version)
                .EnableContinuousIntegrationBuild()
                .EnableNoRestore());
        });

    private string GetVersionPrefix()
    {
        var dt = DateTimeNow();
        var main = $"{dt:yyyy}.{(dt.Month - 1) / 3 + 1}{dt:MM}.{dt:dd}";

        if (Repository.Branch == MainBranch && NuGetVersion != null && NuGetVersion.Version != null)
        {
            //Major=2023, Minor=307, Build=17, Revision=0
            var mmb =$"{NuGetVersion.Version.Major}.{NuGetVersion.Version.Minor}.{NuGetVersion.Version.Build:00}";
            if (mmb == main && NuGetVersion.Version.Revision != 0)
            {
                main = $"{main}.{NuGetVersion.Revision + 1}";
            }
        }

        return main;
    }

    private string GetVersionSuffix()
    {
        return Repository.Branch?.ToLower() switch
        {
            PreviewBranch => $"-{PreviewBranch}{DateTimeNow():HHmmss}",
            _ => null
        };
    }

    private static DateTimeOffset DateTimeNow()
    {
        return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
    }

    private Target Copy => _ => _
        .Description("Copy NuGet Package")
        .OnlyWhenStatic(() => IsServerBuild && Configuration.Equals(Configuration.Release))
        .DependsOn(Compile)
        .Executes(() =>
        {
            OutputDirectory.GlobFiles("**/*.nupkg", "**/*.snupkg")
                .ForEach(x => CopyFileToDirectory(x, ArtifactsDirectory / "packages", FileExistsPolicy.OverwriteIfNewer));
        });

    private Target Artifacts => _ => _
        .DependsOn(Copy)
        .OnlyWhenStatic(() => IsServerBuild)
        .Description("Artifacts Upload")
        .Produces(ArtifactsDirectory / "**/*")
        .Executes(() =>
        {
            Log.Information("Artifacts uploaded.");
        });

    private Target Push => _ => _
        .Description("Push NuGet Package")
        .OnlyWhenStatic(() => IsServerBuild && Configuration.Equals(Configuration.Release))
        .DependsOn(Copy)
        .Requires(() => NuGetApiKey)
        .Requires(() => MyGetApiKey)
        .Executes(() =>
        {
            (ArtifactsDirectory / "packages")
                .GlobFiles("**/*.nupkg")
                .ForEach(Nuget);
        });

    private Target Deploy => _ => _
        .Description("Deploy")
        .DependsOn(Push, Artifacts, Release)
        .Executes(() =>
        {
            Log.Information("Deployed");
        });

    private void Nuget(AbsolutePath x)
    {
        Nuget(x, "https://www.myget.org/F/godsharp/api/v3/index.json", MyGetApiKey);
        Nuget(x, "https://api.nuget.org/v3/index.json", NuGetApiKey);
    }

    private void Nuget(string x, string source, string key) =>
        DotNetNuGetPush(s => s
            .SetTargetPath(x)
            .SetSource(source)
            .SetApiKey(key)
            .EnableSkipDuplicate()
            .EnableNoServiceEndpoint()
        );
}