using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

static class CldApiUtils
{
    // size of memory block needed for CF_PLACEHOLDER_INFO (basic 388, standard 420)
    private const int BLOB_LENGTH = 500;
    public const int FILE_NOT_FOUND = -2147024894;
    public const int NOT_A_CLOUD_FILE = -2147024520;

    public static CF_PLACEHOLDER_BASIC_INFO GetBasicInfo(string fullPath)
    {
        SafeHCFFILE? handle = null;
        nint pointer = IntPtr.Zero;
        try
        {
            handle = GetFileHnadle(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE);

            pointer = AllocMem(BLOB_LENGTH);

            // can not use generic variant of CfGetPlaceholderInfo<T>, because it prohibits the app from terminating normally (hangs on return)
            HRESULT hresInfo = CfGetPlaceholderInfo(handle.DangerousGetHandle(), CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_BASIC, pointer, BLOB_LENGTH, out uint returnedLength);
            PlaceholderExceptionGen(hresInfo, fullPath);

            CF_PLACEHOLDER_BASIC_INFO info = Marshal.PtrToStructure<CF_PLACEHOLDER_BASIC_INFO>(pointer);
            info.FileIdentity = Encoding.Unicode.GetBytes(Marshal.PtrToStringAuto((nint)(pointer + returnedLength - info.FileIdentityLength), (int)info.FileIdentityLength / 2) ?? "");

            return info;
        }
        finally
        {
            GetInfoFreeResources(pointer, handle);
        }
    }

    public static CF_PLACEHOLDER_STANDARD_INFO GetStandardInfo(string fullPath)
    {
        SafeHCFFILE? handle = null;
        nint pointer = IntPtr.Zero;
        try
        {
            handle = GetFileHnadle(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE);

            pointer = AllocMem(BLOB_LENGTH);

            // can not use generic variant of CfGetPlaceholderInfo<T>, because it prohibits the app from terminating normally (hangs on return)
            HRESULT hresInfo = CfGetPlaceholderInfo(handle.DangerousGetHandle(), CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_STANDARD, pointer, BLOB_LENGTH, out uint returnedLength);
            PlaceholderExceptionGen(hresInfo, fullPath);

            CF_PLACEHOLDER_STANDARD_INFO info = Marshal.PtrToStructure<CF_PLACEHOLDER_STANDARD_INFO>(pointer);
            info.FileIdentity = Encoding.Unicode.GetBytes(Marshal.PtrToStringAuto((nint)(pointer + returnedLength - info.FileIdentityLength), (int)info.FileIdentityLength / 2) ?? "");

            return info;
        }
        finally
        {
            GetInfoFreeResources(pointer, handle);
        }
    }

    public static void SetInSyncState(string fullPath)
    {
        SafeHCFFILE? handle = null;
        try
        {
            handle = GetFileHnadle(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS);

            HRESULT hresSync = CfSetInSyncState(handle.DangerousGetHandle(), CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
            PlaceholderExceptionGen(hresSync, fullPath);
        }
        finally
        {
            if (handle != null && !handle.IsInvalid)
            {
                handle.Dispose();
            }
        }
    }

    private static SafeHCFFILE GetFileHnadle(string fullPath, CF_OPEN_FILE_FLAGS flags)
    {
        HRESULT hresHandle = CfOpenFileWithOplock(fullPath, flags, out SafeHCFFILE handle);
        if (hresHandle == FILE_NOT_FOUND)
        {
            throw new FileNotFoundException($"File not found: {fullPath}");
        }
        else if (hresHandle != HRESULT.S_OK)
        {
            throw new Exception($"CfOpenFileWithOplock PATH: {fullPath} \n {hresHandle} int value: {((int)hresHandle)}");
        }
        return handle;
    }

    private static void PlaceholderExceptionGen(HRESULT hres, string fullPath)
    {
        if (hres == NOT_A_CLOUD_FILE)
        {
            throw new OnedataDrive.ErrorHandling.NotPlaceholder($"PATH: {fullPath}");
        }
        else if (hres != HRESULT.S_OK)
        {
            throw new Exception($"CfGetPlaceholderInfo PATH: {fullPath} \n" + hres);
        }
    }

    private static nint AllocMem(int size)
    {
        nint pointer = IntPtr.Zero;
        pointer = Marshal.AllocCoTaskMem(size);
        if (pointer == IntPtr.Zero)
        {
            throw new Exception("Memory allocation failed");
        }
        return pointer;
    }

    private static void GetInfoFreeResources(nint pointer, SafeHCFFILE? handle)
    {
        if (pointer != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(pointer);
        }

        if (handle != null && !handle.IsInvalid)
        {
            handle.Dispose();
        }
    }
}