using System.Runtime.InteropServices;
using static Vanara.PInvoke.CldApi;
using LARGE_INTEGER = System.Int64;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
namespace OnedataDrive
{
    public class PlaceholderData
    {
        public FileIdentity FileIdentity;
        public long Size;
        public long Atime;
        public long Mtime;
        public long Ctime;
        public string Name;
        public FileTypeOD Type;

        public PlaceholderData(FileIdentity FileIdentity, string Name, long Size, long Atime, long Mtime, long Ctime, FileTypeOD Type = FileTypeOD.REG)
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
            this.FileIdentity = new FileIdentity(fileAttribute.file_id, fileAttribute.fileType);
            this.Atime = fileAttribute.atime;
            this.Mtime = fileAttribute.mtime;
            this.Ctime = fileAttribute.ctime;
            this.Name = fileAttribute.name;
            this.Type = fileAttribute.fileType;
        }

        public const int ENCODING_SIZE = 2;

        public static CF_PLACEHOLDER_CREATE_INFO CreateRegInfo(PlaceholderData data)
        {
            UnmanagedMem fileIdentityMem = new((uint)Marshal.SizeOf(data.FileIdentity));
            Marshal.StructureToPtr(data.FileIdentity, fileIdentityMem.GetPointer(), false);
            CF_PLACEHOLDER_CREATE_INFO info = new()
            {
                FileIdentity = fileIdentityMem.GetPointer(),
                FileIdentityLength = fileIdentityMem.GetSize(),
                RelativeFileName = data.Name,
                Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC | CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_SUPERSEDE,
                FsMetadata = CreateFSMetadata(data)
            };
            return info;
        }

        public static CF_PLACEHOLDER_CREATE_INFO CreateDirInfo(PlaceholderData data)
        {
            UnmanagedMem fileIdentityMem = new((uint)Marshal.SizeOf(data.FileIdentity));
            Marshal.StructureToPtr(data.FileIdentity, fileIdentityMem.GetPointer(), false);
            CF_PLACEHOLDER_CREATE_INFO info = new()
            {
                FileIdentity = fileIdentityMem.GetPointer(),
                FileIdentityLength = fileIdentityMem.GetSize(),
                RelativeFileName = data.Name,
                Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC | CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_SUPERSEDE,
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
            if (data.Type == FileTypeOD.REG)
            {
                return CreateRegInfo(data);
            }
            else if (data.Type == FileTypeOD.DIR)
            {
                return CreateDirInfo(data);
            }
            throw new ArgumentException($"Unknown placeholder type: {data.Type}, placeholder name: {data.Name}");
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

        public static PlaceholderCreateInfoList FetchPlaceholdersInfo(string folderPath)
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
            PlaceholderCreateInfoList placeholderCreateInfo = new();
            HashSet<string> placeholderNames = new();
            foreach (Child child in children.children)
            {
                string windowsCorrectName = NameConvertor.DistinctWindowsName(child, placeholderNames);
                PlaceholderData data = new(new FileIdentity(child.fileId, child.fileType), 
                    windowsCorrectName, child.size, child.atime, child.mtime, child.ctime, child.fileType);
                placeholderCreateInfo.Add(CreateInfo(data));
                placeholderNames.Add(windowsCorrectName.ToLower());
            }
            return placeholderCreateInfo;
        }
    }
}