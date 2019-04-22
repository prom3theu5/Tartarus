using System.IO;
using Tartarus.Models;

namespace Tartarus.Features.Downloader
{
    public class ProgramDownloader
    {
        public static void DownloadCefSharpEnvIfNeeded(Registry reg)
        {
            if (Directory.Exists(reg.CefSharpEnvPath)) return;
            BeginDownloadProcess(reg);
        }
        
        private static void BeginDownloadProcess(Registry registry)
        {
            _ = new Views.CefDownloadView(registry)
                .ShowDialog();
        }
    }
}