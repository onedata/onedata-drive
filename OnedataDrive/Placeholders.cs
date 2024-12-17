using System.Runtime.InteropServices;
using static Vanara.PInvoke.CldApi;
using LARGE_INTEGER = System.Int64;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using OnedataDrive.JSON_Object;
namespace OnedataDrive
{
    class PlaceholderData
    {
        public string FileIdentity;
        public long Size;
        public long Atime;
        public long Mtime;
        public long Ctime;
        public string Name;

        public PlaceholderData(string FileIdentity, string Name, long Size, long Atime, long Mtime, long Ctime)
        {
            this.Size = Size;
            this.FileIdentity = FileIdentity;
            this.Atime = Atime;
            this.Mtime = Mtime;
            this.Ctime = Ctime;
            this.Name = Name;
        }
    }

    class Placeholders
    {
        public const int ENCODING_SIZE = 2;

        public static CF_PLACEHOLDER_CREATE_INFO createInfo(PlaceholderData data)
        {
            DateTime ctime = DateTimeOffset.FromUnixTimeSeconds(data.Ctime).UtcDateTime;
            DateTime mtime = DateTimeOffset.FromUnixTimeSeconds(data.Mtime).UtcDateTime;
            DateTime atime = DateTimeOffset.FromUnixTimeSeconds(data.Atime).UtcDateTime;

            CF_PLACEHOLDER_CREATE_INFO info = new()
            {
                FileIdentity = Marshal.StringToCoTaskMemUni(data.FileIdentity),
                FileIdentityLength = (uint)(data.FileIdentity.Length * Marshal.SizeOf(data.FileIdentity[0])) * ENCODING_SIZE,
                RelativeFileName = data.Name,
                Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC,
                FsMetadata = CreateFSMetadata(data)
            };
            return info;
        }

        public static CF_PLACEHOLDER_CREATE_INFO createDirInfo(PlaceholderData data)
        {
            DateTime ctime = DateTimeOffset.FromUnixTimeSeconds(data.Ctime).UtcDateTime;
            DateTime mtime = DateTimeOffset.FromUnixTimeSeconds(data.Mtime).UtcDateTime;
            DateTime atime = DateTimeOffset.FromUnixTimeSeconds(data.Atime).UtcDateTime;

            CF_PLACEHOLDER_CREATE_INFO info = new()
            {
                FileIdentity = Marshal.StringToCoTaskMemUni(data.FileIdentity),
                FileIdentityLength = (uint)(data.FileIdentity.Length * Marshal.SizeOf(data.FileIdentity[0])) * ENCODING_SIZE,
                RelativeFileName = data.Name,
                Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC | CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_DISABLE_ON_DEMAND_POPULATION,
                FsMetadata = CreateFSMetadata(data, directory: true)
            };
            return info;
        }

        public static CF_FS_METADATA CreateFSMetadata(FileAttribute fileAttribute, bool directory = false)
        {
            DateTime ctime = DateTimeOffset.FromUnixTimeSeconds(fileAttribute.ctime).UtcDateTime;
            DateTime mtime = DateTimeOffset.FromUnixTimeSeconds(fileAttribute.mtime).UtcDateTime;
            DateTime atime = DateTimeOffset.FromUnixTimeSeconds(fileAttribute.atime).UtcDateTime;

            return new CF_FS_METADATA
            {
                FileSize = directory ? 0 : fileAttribute.size,
                BasicInfo = new Kernel32.FILE_BASIC_INFO
                {
                    CreationTime = new FILETIME
                    {
                        dwHighDateTime = (int)ctime.ToFileTime().HighPart(),
                        dwLowDateTime = (int)ctime.ToFileTime().LowPart()
                    },
                    LastWriteTime = new FILETIME
                    {
                        dwHighDateTime = (int)mtime.ToFileTime().HighPart(),
                        dwLowDateTime = (int)mtime.ToFileTime().LowPart()
                    },
                    LastAccessTime = new FILETIME
                    {
                        dwHighDateTime = (int)atime.ToFileTime().HighPart(),
                        dwLowDateTime = (int)atime.ToFileTime().LowPart()
                    },
                    ChangeTime = new FILETIME
                    {
                        dwHighDateTime = (int)mtime.ToFileTime().HighPart(),
                        dwLowDateTime = (int)mtime.ToFileTime().LowPart()
                    },
                    FileAttributes = directory ? FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY : FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL
                }
            };
        }

        public static CF_FS_METADATA CreateFSMetadata(PlaceholderData data, bool directory = false)
        {
            DateTime ctime = DateTimeOffset.FromUnixTimeSeconds(data.Ctime).UtcDateTime;
            DateTime mtime = DateTimeOffset.FromUnixTimeSeconds(data.Mtime).UtcDateTime;
            DateTime atime = DateTimeOffset.FromUnixTimeSeconds(data.Atime).UtcDateTime;

            return new CF_FS_METADATA
            {
                FileSize = directory ? 0 : data.Size,
                BasicInfo = new Kernel32.FILE_BASIC_INFO
                {
                    CreationTime = new FILETIME
                    {
                        dwHighDateTime = (int)ctime.ToFileTime().HighPart(),
                        dwLowDateTime = (int)ctime.ToFileTime().LowPart()
                    },
                    LastWriteTime = new FILETIME
                    {
                        dwHighDateTime = (int)mtime.ToFileTime().HighPart(),
                        dwLowDateTime = (int)mtime.ToFileTime().LowPart()
                    },
                    LastAccessTime = new FILETIME
                    {
                        dwHighDateTime = (int)atime.ToFileTime().HighPart(),
                        dwLowDateTime = (int)atime.ToFileTime().LowPart()
                    },
                    ChangeTime = new FILETIME
                    {
                        dwHighDateTime = (int)mtime.ToFileTime().HighPart(),
                        dwLowDateTime = (int)mtime.ToFileTime().LowPart()
                    },
                    FileAttributes = directory ? FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY : FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL
                }
            };
        }
    }
}