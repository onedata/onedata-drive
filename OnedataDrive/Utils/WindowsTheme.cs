using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace OnedataDrive.CloudSync.Utils
{
    internal class WindowsTheme
    {
        public bool IsSystemDarkMode(bool defaultValue = true)
        {
            
            const string REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string REGISTRY_NAME = "SystemUsesLightTheme";

            RegistryKey? key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY);
            
            if (key is not null)
            {
                object? value = key.GetValue(REGISTRY_NAME);
                return value is int i && i == 0;
            }
            
            return defaultValue;
            
        }
    }
}
