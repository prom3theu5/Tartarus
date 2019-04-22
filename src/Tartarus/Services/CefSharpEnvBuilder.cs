using System;
using System.IO;
using System.Threading.Tasks;
using Tartarus.Common;
using Tartarus.Helpers;
using Tartarus.Models;

namespace Tartarus.Services
{
    public class CefSharpEnvBuilder
    {
        private readonly Registry _registry;

        public CefSharpEnvBuilder(Registry registry)
        {
            _registry = registry;
        }

        public async Task Do()
        {
            var settings = new ComposeSettings(_registry);
            Init(settings);
            await StepGetNugetPackages(settings);
            StepExtractNugets(settings);
            await StepCopyFiles(settings);
            Clean(settings);
        }


        private static void Init(ComposeSettings settings)
        {
            Io.CreateDirIfNotExist(settings.CefSharpEnvPath);
            Io.CreateDirIfNotExist(settings.TmpDownloadPath);
            Io.CreateDirIfNotExist(settings.TmpExtractionPath);
        }


        private static void Clean(ComposeSettings settings)
        {
            Io.RemoveFolder(settings.TmpDownloadPath);
            Io.RemoveFolder(settings.TmpExtractionPath);
            Io.RemoveFolder(settings.TmpPath);
        }

        private async Task StepGetNugetPackages(ComposeSettings settings)
        {
            foreach (var p in settings.Nugets)
            {
                var file = $"{p.Name}.{p.Version}.nupkg";
                var path = Path.Combine(settings.LocalNugetSourcePath, p.Name, p.Version);
                var local = Path.Combine(path, file);
                if (File.Exists(local) && settings.UseLocalNugetSource)
                {
                    var dst = Path.Combine(settings.TmpDownloadPath, file);
                    await Io.CopyFileAsync(local, dst);
                }
                else
                {
                    await DownloadOneNuget(settings, p, file);
                }
            }
        }

        private void StepExtractNugets(ComposeSettings settings)
        {
            foreach (var p in settings.Nugets)
            {
                var dir = $"{p.Name}.{p.Version}";
                var file = $"{dir}.nupkg";
                var src = Path.Combine(settings.TmpDownloadPath, file);
                var dst = Path.Combine(settings.TmpExtractionPath, dir);
                Io.CreateDirIfNotExist(dst);
                ExtractTool.ExtractZipToDirectory(src, dst);
            }
        }

        private async Task StepCopyFiles(ComposeSettings settings)
        {
            Io.CreateDirIfNotExist(settings.CefSharpEnvPath);
            var copyWorker = new CopyWorkerService(settings);
            foreach (var p in settings.Nugets)
            {
                await copyWorker.CopyOne(p);
            }
        }

        private async Task DownloadOneNuget(ComposeSettings settings, NugetInfo n, string fileName)
        {
            var dl = new DownloadTool();
            var url = $"https://www.nuget.org/api/v2/package/{n.Name}/{n.Version}";
            var dstFile = Path.Combine(settings.TmpDownloadPath, fileName);
            if (File.Exists(dstFile) == false)
                await dl.DownloadFileAsync(url, dstFile);
        }
    }
}