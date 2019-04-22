using System;
using System.Linq;
using GitAutoVersionTool;
using Helpers;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

class Build : NukeBuild
{
    static readonly DateTime _buildDate = DateTime.UtcNow;
    const string AssemblyName = "Tartarus.exe";

    [Parameter("Build counter from outside environment", Name = "build")] readonly int BuildCounter;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)", Name = "config")]
    readonly string Configuration = "Release";


    [Solution("Tartarus.sln")] readonly Solution Solution;
    MSBuildTargetPlatform Platform = MSBuildTargetPlatform.x64;

    GitAutoVersion Version => GitAutoVersionFactory.Create(BuildCounter, _buildDate);

    Project MainProject => Solution.GetProject("Tartarus").NotNull();

    AbsolutePath SourceDir => RootDirectory / "src";
    AbsolutePath ToolsDir => RootDirectory / "tools";
    AbsolutePath ArtifactsDir => RootDirectory / "_artifacts";
    AbsolutePath TmpBuild => TemporaryDirectory / "build";
    AbsolutePath LibzPath => ToolsDir / "LibZ.Tool" / "tools" / "libz.exe";
    AbsolutePath NugetPath => ToolsDir / "nuget" / "nuget.exe";

    AbsolutePath SevenZipPath => ToolsDir / "7-Zip.CommandLine" / "tools" / "7za.exe";


    Target Information => _ => _
        .Executes(() =>
        {
            Logger.Info($"SemVer: {Version.SemVersion}");
            Logger.Info($"InformationalVersion: {Version.InformationalVersion}");
            Logger.Info($"AssemblyVersion: {Version.AssemblyVersion}");
            Logger.Info($"FileVersion: {Version.FileVersion}");
        });


    Target CheckTools => _ => _
        .DependsOn(Information)
        .Executes(() =>
        {
            Downloader.DownloadIfNotExists("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", NugetPath,
                "Nuget");
            var toolsNugetFile = ToolsDir / "packages.config";
            using (var process = ProcessTasks.StartProcess(
                NugetPath,
                $"install   {toolsNugetFile} -OutputDirectory {ToolsDir} -ExcludeVersion",
                SourceDir))
            {
                process.AssertWaitForExit();
                ControlFlow.AssertWarn(process.ExitCode == 0,
                    "Nuget restore report generation process exited with some errors.");
            }
        });

    Target Clean => _ => _
        .DependsOn(CheckTools)
        .Executes(() =>
        {
            if (DirectoryExists(TmpBuild))
                DeleteDirectory(TmpBuild);

            if (DirectoryExists(ArtifactsDir))
                DeleteDirectory(ArtifactsDir);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            using (var process = ProcessTasks.StartProcess(
                NugetPath,
                $"restore  {Solution.Path}",
                SourceDir))
            {
                process.AssertWaitForExit();
                ControlFlow.AssertWarn(process.ExitCode == 0,
                    "Nuget restore report generation process exited with some errors.");
            }
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>

        {
            var buildOut = TmpBuild / CommonDir.Build /
                           MainProject.Name;
            EnsureExistingDirectory(buildOut);

            MSBuild(s => s
                .SetToolPath("c:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\MSBuild\\Current\\Bin\\msbuild.exe")
                .SetTargetPath(Solution)
                .SetTargets("Rebuild")
                .SetTargetPlatform(Platform)
                .SetOutDir(buildOut)
                .SetVerbosity(MSBuildVerbosity.Quiet)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(Version.AssemblyVersion)
                .SetFileVersion(Version.FileVersion)
                .SetInformationalVersion(Version.InformationalVersion)
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(IsLocalBuild));
        });

    Target Merge => _ => _
        .DependsOn(Compile)
        .DependsOn(Restore)
        .Executes(() =>

        {
            var doNotMerge = new[]
            {
                "build.dll", "libcef.dll", "chrome_elf.dll", "d3dcompiler_47.dll",
                "libEGL.dll", "libGLESv2.dll", "CefSharp.dll", "CefSharp.Core.dll",
                "CefSharp.BrowserSubprocess.Core.dll"
            };
            var exclude = string.Join(' ', doNotMerge.Select(x => $"--exclude={x}"));

            var buildOut = TmpBuild / CommonDir.Build /
                           MainProject.Name;

            var mergeOut = TmpBuild / CommonDir.Merge /
                           MainProject.Name;

            CopyDirectoryRecursively(buildOut, mergeOut);

            using (var process = ProcessTasks.StartProcess(
                LibzPath,
                $"inject-dll --assembly {AssemblyName} --include *.dll {exclude} --move",
                mergeOut))
            {
                process.AssertWaitForExit();
                ControlFlow.AssertWarn(process.ExitCode == 0,
                    "Libz report generation process exited with some errors.");
            }
        });


    Target CopyToArtifacts => _ => _
        .DependsOn(Merge)
        .Executes(() =>

        {
            var mergeOut = TmpBuild / CommonDir.Merge /
                           MainProject.Name;

            var readyOut = TmpBuild / CommonDir.Ready /
                           MainProject.Name;

            EnsureExistingDirectory(readyOut);
            CopyFile(mergeOut / $"{AssemblyName}", ArtifactsDir / $"{AssemblyName}");
        });

    Target CleanOnTheEnd => _ => _
        .DependsOn(CopyToArtifacts)
        .Executes(() =>
        {
            DeleteDirectory(TmpBuild);
        });

    public static int Main() => Execute<Build>(x => x.CleanOnTheEnd);
}