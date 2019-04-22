using Chromely.CefSharp.Winapi.BrowserWindow;
using Chromely.Core;
using Chromely.Core.Helpers;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Tartarus.Features.Downloader;
using Tartarus.Helpers;
using Tartarus.Models;

namespace Tartarus
{
    public partial class App : Application
    {
        private static readonly AppEnvironment _env = AppEnvironmentBuilder.Instance.GetAppEnvironment();
        private static readonly Config _cnf = ConfigBuilder.Create();
        private static readonly Registry _reg = new Registry(_env, _cnf);

        public App()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Init();
            Start();
        }

        private void Start()
        {
            const string startUrl = "https://google.com";

            var config = ChromelyConfiguration
                .Create()
                .WithCustomSetting(CefSettingKeys.BrowserSubprocessPath, _reg.BrowserSubprocessPath)
                .WithCustomSetting(CefSettingKeys.LocalesDirPath, _reg.CefSharpLocalePath)
                .WithCustomSetting(CefSettingKeys.LogFile, ".logs\\chronium.log")
                .UseDefaultLogger(".logs\\chromely.log")
                .WithHostMode(Chromely.Core.Host.WindowState.Normal)
                .WithHostTitle("Tartarus: City of Heroes Launcher")
                .WithHostSize(1100, 700)
                .WithStartUrl(startUrl);

            using (var window = new CefSharpBrowserWindow(config))
            {
                window.Run(new[] {""});
            }

            Current.Shutdown();
        }

        private void Init()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            ProgramDownloader.DownloadCefSharpEnvIfNeeded(_reg);
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var dll = args.Name.Split(new[] { ',' }, 2)[0] + ".dll";
            switch (dll)
            {
                case "CefSharp.Core.dll":
                case "CefSharp.dll":
                    var path = Path.Combine(_reg.CefSharpEnvPath, dll);
                    var asm = Assembly.LoadFile(path);
                    return asm;
                default:
                    return null;
            }
        }
    }
}
