using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.Storage;
using Windows.Storage.Provider;

namespace OnedataDrive.Utils
{
    public class CustomStatus
    {
        public static async void ApplyCustomStatus(string path)
        {
            // 1. Create a container for the properties
            List<StorageProviderItemProperty> itemProperties = new List<StorageProviderItemProperty>();

            // 2. Define a custom status property
            StorageProviderItemProperty customStatus = new StorageProviderItemProperty()
            {
                // The ID matching the property registered with your Sync Root
                Id = 1,

                // The visible text displayed in File Explorer (e.g., in a custom column)
                Value = "File will not be synced with the cloud",

                // Path to your icon resource dll/exe. Do not leave empty to prevent Explorer crashes.
                IconResource = "shell32.dll,131"
            };

            itemProperties.Add(customStatus);

            try
            { 
                if (Directory.Exists(path))
                {
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                    await StorageProviderItemProperties.SetAsync(folder, itemProperties);
                }
                else
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                    await StorageProviderItemProperties.SetAsync(file, itemProperties);
                }
                
                Debug.Print("Custom status applied successfully.");
            }
            catch (Exception ex)
            {
                Debug.Print($"Failed to set status: {ex.Message}");
            }
        }
    }
}
