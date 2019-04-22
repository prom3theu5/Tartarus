using System;
using System.Net;
using System.Threading.Tasks;

namespace Tartarus.Helpers
{
    public class DownloadTool
    {
        public async Task DownloadFileAsync(string fileUrl, string dst)
        {
            var downloadLink = new Uri(fileUrl);

            using (var webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(downloadLink, dst);
            }
        }
    }
}