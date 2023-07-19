using MimeMapping;

using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;

using Octokit;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using static Nuke.Common.Tools.GitHub.GitHubTasks;

[GitHubActions(
    "build",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    PublishArtifacts = true,
    EnableGitHubToken = true,
    OnPushBranches = new[] { DevelopBranch },
    InvokedTargets = new[] { nameof(Compile) },
    CacheKeyFiles = new string[0]
)]
[GitHubActions(
    "deploy",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    PublishArtifacts = true,
    EnableGitHubToken = true,
    OnPushBranches = new[] { MainBranch, PreviewBranch },
    InvokedTargets = new[] { nameof(Deploy) },
    ImportSecrets = new[] { nameof(NuGetApiKey), nameof(MyGetApiKey) },
    CacheKeyFiles = new string[0]
)]
internal partial class Build
{
    private GitHubActions GitHubActions => GitHubActions.Instance;

    private Target Release => _ => _
        .Description("Release")
        .DependsOn(Artifacts)
        .Executes(async () =>
        {
            var tag = $"v{Version}";
            GitHubTasks.GitHubClient.Credentials ??= new Credentials(GitHubActions.Token.NotNull());
            var release = await GitHubTasks.GitHubClient.Repository.Release.Create(
                Repository.GetGitHubOwner(),
                Repository.GetGitHubName(),
                new NewRelease(tag)
                {
                    Name = tag,
                    Prerelease = true,
                    Draft = true,
                    Body = $"Release v{Version} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                });

            var uploads = ArtifactsDirectory.GlobFiles("**/*").NotNull().Select(async x =>
            {
                await using var assetFile = File.OpenRead(x);
                var asset = new ReleaseAssetUpload
                {
                    FileName = x.Name,
                    ContentType =  MimeUtility.GetMimeMapping(x),
                    RawData = assetFile
                };
                await GitHubTasks.GitHubClient.Repository.Release.UploadAsset(release, asset);
            }).ToArray();

            Task.WaitAll(uploads);
        });
}