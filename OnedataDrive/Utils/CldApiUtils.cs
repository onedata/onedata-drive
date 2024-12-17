using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

static class CldApiUtils
{
    // size of memory block needed for CF_PLACEHOLDER_INFO (basic 388, standard 420)
    private const int BLOB_LENGTH = 500;

    public static CF_PLACEHOLDER_BASIC_INFO GetBasicInfo(string fullPath)
    {
        SafeHCFFILE handle;
        HRESULT hres = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE, out handle);
        if (hres != HRESULT.S_OK)
        {
            throw new Exception($"CfOpenFileWithOplock PATH: {fullPath} \n" + hres);
        }
        nint pointer = Marshal.AllocCoTaskMem(BLOB_LENGTH);
        // can not use generic variant, because it prohibits the app from terminating normally (hangs on return)
        HRESULT hres2 = CfGetPlaceholderInfo(handle.DangerousGetHandle(), CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_BASIC, pointer, BLOB_LENGTH, out uint returnedLength);
        if (hres != HRESULT.S_OK)
        {
            throw new Exception($"CfGetPlaceholderInfo PATH: {fullPath} \n" + hres);
        }
        CF_PLACEHOLDER_BASIC_INFO info = Marshal.PtrToStructure<CF_PLACEHOLDER_BASIC_INFO>(pointer);
        info.FileIdentity = Encoding.Unicode.GetBytes(Marshal.PtrToStringAuto((nint) (pointer + returnedLength - info.FileIdentityLength), (int)info.FileIdentityLength / 2) ?? "");

        CfCloseHandle(handle);
        Marshal.FreeCoTaskMem(pointer);

        return info;
    }

    public static CF_PLACEHOLDER_STANDARD_INFO GetStandardInfo(string fullPath)
    {
        SafeHCFFILE handle;
        HRESULT hres = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE, out handle);
        if (hres != HRESULT.S_OK)
        {
            throw new Exception($"CfOpenFileWithOplock PATH: {fullPath} \n" + hres);
        }
        nint pointer = Marshal.AllocCoTaskMem(BLOB_LENGTH);
        // can not use generic variant, because it prohibits the app from terminating normally (hangs on return)
        HRESULT hres2 = CfGetPlaceholderInfo(handle.DangerousGetHandle(), CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_STANDARD, pointer, BLOB_LENGTH, out uint returnedLength);
        if (hres != HRESULT.S_OK)
        {
            throw new Exception($"CfGetPlaceholderInfo PATH: {fullPath} \n" + hres);
        }
        CF_PLACEHOLDER_STANDARD_INFO info = Marshal.PtrToStructure<CF_PLACEHOLDER_STANDARD_INFO>(pointer);
        info.FileIdentity = Encoding.Unicode.GetBytes(Marshal.PtrToStringAuto((nint) (pointer + returnedLength - info.FileIdentityLength), (int)info.FileIdentityLength / 2) ?? "");

        CfCloseHandle(handle);
        Marshal.FreeCoTaskMem(pointer);

        return info;
    }
}