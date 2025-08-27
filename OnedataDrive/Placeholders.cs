using System.Runtime.InteropServices;
using static Vanara.PInvoke.CldApi;
using LARGE_INTEGER = System.Int64;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
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
        public string Type;

        public const string REGULAR_FILE = "REG";
        public const string DIRECTORY = "DIR";

        public PlaceholderData(string FileIdentity, string Name, long Size, long Atime, long Mtime, long Ctime, string Type = REGULAR_FILE)
        {
            this.Size = Size;
            this.FileIdentity = FileIdentity;
            this.Atime = Atime;
            this.Mtime = Mtime;
            this.Ctime = Ctime;
            this.Name = Name;
            this.Type = Type;
        }

        public PlaceholderData(FileAttribute fileAttribute)
        {
            this.Size = fileAttribute.size;
            this.FileIdentity = fileAttribute.file_id;
            this.Atime = fileAttribute.atime;
            this.Mtime = fileAttribute.mtime;
            this.Ctime = fileAttribute.ctime;
            this.Name = fileAttribute.name;
            this.Type = fileAttribute.type;
        }
    }

    class Placeholders
    {
        public const int ENCODING_SIZE = 2;

        public static CF_PLACEHOLDER_CREATE_INFO CreateRegInfo(PlaceholderData data)
        {
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

        public static CF_PLACEHOLDER_CREATE_INFO CreateDirInfo(PlaceholderData data)
        {
            CF_PLACEHOLDER_CREATE_INFO info = new()
            {
                FileIdentity = Marshal.StringToCoTaskMemUni(data.FileIdentity),
                FileIdentityLength = (uint)(data.FileIdentity.Length * Marshal.SizeOf(data.FileIdentity[0])) * ENCODING_SIZE,
                RelativeFileName = data.Name,
                Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC,
                FsMetadata = CreateFSMetadata(data, directory: true)
            };
            return info;
        }

        /// <summary>
        /// Creates CF_PLACEHOLDER_CREATE_INFO structure and selects type (REG, DIR) based on type in PlaceholderData.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static CF_PLACEHOLDER_CREATE_INFO CreateInfo(PlaceholderData data)
        {
            if (data.Type == PlaceholderData.REGULAR_FILE)
            {
                return CreateRegInfo(data);
            }
            else if (data.Type == PlaceholderData.DIRECTORY)
            {
                return CreateDirInfo(data);
            }
            throw new ArgumentException("Unknown placeholder type: " + data.Type);
        }

        public static CF_FS_METADATA CreateFSMetadata(FileAttribute fileAttribute, bool directory = false)
        {
            PlaceholderData data = new PlaceholderData(fileAttribute);
            return CreateFSMetadata(data, directory);
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

        public static PlaceholderCreateInfo FetchPlaceholdersInfo(string folderPath)
        {
            // get folder id
            string id = PathUtils.GetPlaceholderId(folderPath);
            // get space record
            SpaceFolder space = PathUtils.GetSpaceFolder(folderPath);
            // get placeholder info
            var task = RestClient.GetFilesAndSubdirs(id, space.providerInfos);
            task.Wait();
            DirChildren children = task.Result;
            // create array
            PlaceholderCreateInfo placeholderCreateInfo = new();
            foreach (Child child in children.children)
            {
                string windowsCorrectName = NameConvertor.DistinctWindowsName(child, placeholderCreateInfo);
                PlaceholderData data = new(child.file_id, windowsCorrectName, child.size, child.atime, child.mtime, child.ctime, child.type);
                placeholderCreateInfo.Add(CreateInfo(data));
            }
            return placeholderCreateInfo;
        }
    }
}