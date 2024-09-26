using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudSyncGUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Closed += OnWindowClosed;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Config config = new() {
                zone_host = zoneHostTbx.Text,
                provider_token = tokenTbx.Text,
                root_path = rootPathTbx.Text
            };

            if (!config.IsComplete())
            {
                statusTbl.Text = "Wrong data";
                return;
            }

            int status = await Task.Run(() => CloudSync.Run(config, delete: true));
            if (status == 0)
            {
                statusTbl.Text = "Connected";
            }
            //myButton.Content = "Clicked";
            /*
            CloudProvider.RegisterWithShell("C:\\Users\\User\\Desktop\\root");
            Thread.Sleep(10000);
            CloudProvider.UnregisterSafely();
            */
        }

        private void OnWindowClosed(object sender, WindowEventArgs e)
        {
            CloudSync.Stop();
        }
    }
}
