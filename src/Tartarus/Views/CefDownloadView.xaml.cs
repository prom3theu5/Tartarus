using System;
using System.Windows;
using Tartarus.Models;
using Tartarus.Services;

namespace Tartarus.Views
{
    public partial class CefDownloadView : Window
    {
        private readonly Registry _registery;
        public CefDownloadView(Registry registry)
        {
            _registery = registry;
            InitializeComponent();
            Loaded += CefDownloadView_Loaded;
        }

        private async void CefDownloadView_Loaded(object sender, RoutedEventArgs e)
        {
            var cefSharpEnvBuilder = new CefSharpEnvBuilder(_registery);
            await cefSharpEnvBuilder.Do();

            Close();
        }
    }
}
