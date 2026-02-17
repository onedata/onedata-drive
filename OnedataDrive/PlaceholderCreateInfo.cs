using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive
{
    public class PlaceholderCreateInfo : IDisposable
    {
        private List<CF_PLACEHOLDER_CREATE_INFO> infos;

        public PlaceholderCreateInfo()
        {
            infos = new();
        }
        public void Add(CF_PLACEHOLDER_CREATE_INFO info)
        {
            infos.Add(info);
        }

        public ReadOnlyCollection<CF_PLACEHOLDER_CREATE_INFO> Get()
        {
            return infos.AsReadOnly();
        }

        public CF_PLACEHOLDER_CREATE_INFO[] GetArray()
        {
            return infos.ToArray();
        }

        public int Count()
        {
            return infos.Count;
        }

        public void Dispose()
        {
            infos.ForEach(i =>
            {
                Marshal.FreeCoTaskMem(i.FileIdentity);
                i.FileIdentity = IntPtr.Zero;
            });
            infos.Clear();
        }
    }
}