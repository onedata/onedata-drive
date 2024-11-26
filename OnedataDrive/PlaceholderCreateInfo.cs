using System.Runtime.InteropServices;
using static Vanara.PInvoke.CldApi;

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

    public List<CF_PLACEHOLDER_CREATE_INFO> Get()
    {
        return infos;
    }

    public CF_PLACEHOLDER_CREATE_INFO[] GetArray()
    {
        return infos.ToArray();
    }

    public void Dispose()
    {
        infos.ForEach(i => {
            Marshal.FreeCoTaskMem(i.FileIdentity);
            i.FileIdentity = IntPtr.Zero;
            });
    }
}