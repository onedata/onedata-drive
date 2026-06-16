using System.Runtime.InteropServices;
using static Vanara.PInvoke.CldApi;
using Windows.Storage.Provider;
using OnedataDrive.ErrorHandling;
using Windows.Storage;

namespace OnedataDrive
{
    [Guid("441D329F-009F-4E58-B05B-2D9B85A92469")]
    [ComVisible(true)]
    public class MyItemPropertySource : IStorageProviderItemPropertySource
    {
        public IEnumerable<StorageProviderItemProperty> GetItemProperties(string itemPath)
        {
            var properties = new List<StorageProviderItemProperty>();

            // Only for .abc files (case-insensitive)
            if (!itemPath.EndsWith(".abc", StringComparison.OrdinalIgnoreCase))
            {
                var prop = new StorageProviderItemProperty
                {
                    Id = 1, // Matches the definition ID from Step A
                    IconResource = @"C:\Program Files\MySyncEngine\Assets\shared_state.ico",
                    Value = "Shared with team" // Tooltip text fallback
                };
                properties.Add(prop);
            }

            return properties;
        }
    }
}
