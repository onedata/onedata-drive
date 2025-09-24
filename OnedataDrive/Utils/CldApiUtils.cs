using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

static class CldApiUtils
{
    // size of memory block needed for CF_PLACEHOLDER_INFO (basic 388, standard 420)
    private const int BLOB_LENGTH = 500;
    public const int FILE_NOT_FOUND = -2147024894;

    public static CF_PLACEHOLDER_BASIC_INFO GetBasicInfo(string fullPath)
    {
        SafeHCFFILE? handle = null;
        nint pointer = IntPtr.Zero;
        try
        {
            HRESULT hresHandle = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE, out handle);
            if (hresHandle == FILE_NOT_FOUND)
            {
                throw new FileNotFoundException($"File not found: {fullPath}");
            }
            else if (hresHandle != HRESULT.S_OK)
            {
                throw new Exception($"CfOpenFileWithOplock PATH: {fullPath} \n {hresHandle} int value: {((int)hresHandle)}");
            }
            pointer = Marshal.AllocCoTaskMem(BLOB_LENGTH);
            if (pointer == IntPtr.Zero)
            {
                throw new Exception("Memory allocation failed");
            }

            // can not use generic variant of CfGetPlaceholderInfo<T>, because it prohibits the app from terminating normally (hangs on return)
            HRESULT hresInfo = CfGetPlaceholderInfo(handle.DangerousGetHandle(), CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_BASIC, pointer, BLOB_LENGTH, out uint returnedLength);
            if (hresInfo != HRESULT.S_OK)
            {
                throw new Exception($"CfGetPlaceholderInfo PATH: {fullPath} \n" + hresInfo);
            }
            CF_PLACEHOLDER_BASIC_INFO info = Marshal.PtrToStructure<CF_PLACEHOLDER_BASIC_INFO>(pointer);
            info.FileIdentity = Encoding.Unicode.GetBytes(Marshal.PtrToStringAuto((nint)(pointer + returnedLength - info.FileIdentityLength), (int)info.FileIdentityLength / 2) ?? "");

            return info;
        }
        finally
        {
            if (pointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pointer);
            }

            if (handle != null && !handle.IsInvalid)
            {
                handle.Dispose();
                // CfCloseHandle(handle);   ---->>>> DO NOT USE THIS - casuses deadlock <<<<----
            }
        }
    }

    public static CF_PLACEHOLDER_STANDARD_INFO GetStandardInfo(string fullPath)
    {
        SafeHCFFILE? handle = null;
        nint pointer = IntPtr.Zero;
        try
        {
            HRESULT hresHandle = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE, out handle);
            if (hresHandle == FILE_NOT_FOUND)
            {
                throw new FileNotFoundException($"File not found: {fullPath}");
            }
            else if (hresHandle != HRESULT.S_OK)
            {
                throw new Exception($"CfOpenFileWithOplock PATH: {fullPath} \n {hresHandle} int value: {((int)hresHandle)}");
            }
            pointer = Marshal.AllocCoTaskMem(BLOB_LENGTH);
            if (pointer == IntPtr.Zero)
            {
                throw new Exception("Memory allocation failed");
            }

            // can not use generic variant of CfGetPlaceholderInfo<T>, because it prohibits the app from terminating normally (hangs on return)
            HRESULT hresInfo = CfGetPlaceholderInfo(handle.DangerousGetHandle(), CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_STANDARD, pointer, BLOB_LENGTH, out uint returnedLength);
            if (hresInfo != HRESULT.S_OK)
            {
                throw new Exception($"CfGetPlaceholderInfo PATH: {fullPath} \n" + hresInfo);
            }
            CF_PLACEHOLDER_STANDARD_INFO info = Marshal.PtrToStructure<CF_PLACEHOLDER_STANDARD_INFO>(pointer);
            info.FileIdentity = Encoding.Unicode.GetBytes(Marshal.PtrToStringAuto((nint)(pointer + returnedLength - info.FileIdentityLength), (int)info.FileIdentityLength / 2) ?? "");

            return info;
        }
        finally
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

    public static void SetInSyncState(string fullPath)
    {
        SafeHCFFILE? handle = null;
        try
        {
            HRESULT hresHandle = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS, out handle);
            if (hresHandle != HRESULT.S_OK)
            {
                throw new Exception($"CfOpenFileWithOplock PATH: {fullPath} \n {hresHandle} int value: {((int)hresHandle)}");
            }
            HRESULT hresSync = CfSetInSyncState(handle.DangerousGetHandle(), CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
            if (hresSync != HRESULT.S_OK)
            {
                throw new Exception($"CfSetInSyncState PATH: {fullPath} \n {hresSync} int value: {((int)hresSync)}");
            }
        }
        finally
        {
            if (handle != null && !handle.IsInvalid)
            {
                handle.Dispose();
            }
        }
    }
}