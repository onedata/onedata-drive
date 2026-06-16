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
        public static async void ApplyCustomStatusToFile(StorageFile file)
        {
            // 1. Create a container for the properties
            List<StorageProviderItemProperty> itemProperties = new List<StorageProviderItemProperty>();

            // 2. Define a custom status property
            StorageProviderItemProperty customStatus = new StorageProviderItemProperty()
            {
                // The ID matching the property registered with your Sync Root
                Id = 1,

                // The visible text displayed in File Explorer (e.g., in a custom column)
                Value = "No Sync",

                // Path to your icon resource dll/exe. Do not leave empty to prevent Explorer crashes.
                IconResource = "ms-resource:Resource/StatusIcon,0"
            };

            itemProperties.Add(customStatus);

            try
            {
                // 3. Apply the custom status asynchronously to the target item
                await StorageProviderItemProperties.SetAsync(file, itemProperties);
                Debug.Print("Custom status applied successfully.");
            }
            catch (Exception ex)
            {
                Debug.Print($"Failed to set status: {ex.Message}");
            }
        }
    }
}
