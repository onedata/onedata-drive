using OnedataDrive;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

static class CldApiUtils
{
    // size of memory block needed for CF_PLACEHOLDER_INFO (basic 388, standard 420)
    private const int BLOB_LENGTH = 500;
    public const uint NOT_A_CLOUD_FILE = 0x80070178u;

    public static CF_PLACEHOLDER_BASIC_INFO GetBasicInfo(string fullPath)
    {
        using UnmanagedMem memory = new UnmanagedMem(BLOB_LENGTH);
        using CfHandle handle = new CfHandle(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE);

        // can not use generic variant of CfGetPlaceholderInfo<T>, because it prohibits the app from terminating normally (hangs on return)
        HRESULT hresInfo = CfGetPlaceholderInfo(handle.GetDangerousHandle(), CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_BASIC, memory.GetPointer(), BLOB_LENGTH, out uint returnedLength);
        PlaceholderExceptionGen(hresInfo, fullPath);

        CF_PLACEHOLDER_BASIC_INFO info = Marshal.PtrToStructure<CF_PLACEHOLDER_BASIC_INFO>(memory.GetPointer());
        info.FileIdentity = Encoding.Unicode.GetBytes(Marshal.PtrToStringAuto((nint)(memory.GetPointer() + returnedLength - info.FileIdentityLength), (int)info.FileIdentityLength / 2) ?? "");

        return info;
    }

    public static CF_PLACEHOLDER_STANDARD_INFO GetStandardInfo(string fullPath)
    {
        using UnmanagedMem memory = new UnmanagedMem(BLOB_LENGTH);
        using CfHandle handle = new CfHandle(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE);

        // can not use generic variant of CfGetPlaceholderInfo<T>, because it prohibits the app from terminating normally (hangs on return)
        HRESULT hresInfo = CfGetPlaceholderInfo(handle.GetDangerousHandle(), CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_STANDARD, memory.GetPointer(), BLOB_LENGTH, out uint returnedLength);
        PlaceholderExceptionGen(hresInfo, fullPath);

        CF_PLACEHOLDER_STANDARD_INFO info = Marshal.PtrToStructure<CF_PLACEHOLDER_STANDARD_INFO>(memory.GetPointer());
        info.FileIdentity = Encoding.Unicode.GetBytes(Marshal.PtrToStringAuto((nint)(memory.GetPointer() + returnedLength - info.FileIdentityLength), (int)info.FileIdentityLength / 2) ?? "");

        return info;
    }

    public static void SetInSyncState(string fullPath)
    {
        using CfHandle handle = new CfHandle(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS);

        HRESULT hresSync = CfSetInSyncState(handle.GetDangerousHandle(), CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
        PlaceholderExceptionGen(hresSync, fullPath);
    }

    public static string GetFileIdFromPointer(nint pointer, uint length, int characterSize = 2)
    {
        if (length <= 0 || characterSize <= 0 || length % characterSize != 0 || pointer == IntPtr.Zero)
        {
            throw new ArgumentException($"length({length}) <= 0 || characterSize({characterSize}) <= 0 || length % characterSize != 0 || pointer({pointer}) == IntPtr.Zero");
        }
        return Marshal.PtrToStringAuto(pointer, (int)length / characterSize) ?? "";
    }


    public static HRESULT CreatePlaceholders(string baseDirPath, CF_PLACEHOLDER_CREATE_INFO[] files, CF_CREATE_FLAGS createFlags, out uint entriesProcessed)
    {
        if (files.Length <= 0)
        {
            entriesProcessed = 0;
            return HRESULT.S_OK;
        }

        return CfCreatePlaceholders(baseDirPath, files, (uint)files.Length,
                createFlags, out entriesProcessed);
    }

    public static HRESULT CreatePlaceholders(string baseDirPath, List<PlaceholderData> files, CF_CREATE_FLAGS createFlags, out uint entriesProcessed)
    {
        using (PlaceholderCreateInfoList placeholderList = new())
        {
            foreach (PlaceholderData file in files)
            {
                placeholderList.Add(file);
            }

            return CfCreatePlaceholders(baseDirPath, placeholderList.GetArray(), (uint)placeholderList.Count(),
                createFlags, out entriesProcessed);
        }
    }

    public static HRESULT CreatePlaceholders(string baseDirPath, List<FileAttribute> files, CF_CREATE_FLAGS createFlags, out uint entriesProcessed)
    {
        List<PlaceholderData> data = new();
        foreach (FileAttribute file in files)
        {
            data.Add(new PlaceholderData(file));
        }

        return CreatePlaceholders(baseDirPath, data, createFlags, out entriesProcessed);
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
}