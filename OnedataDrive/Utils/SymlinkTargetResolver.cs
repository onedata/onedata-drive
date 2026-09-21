using OnedataDrive.JSON_Object;

namespace OnedataDrive.Utils
{
    public static class SymlinkTargetResolver
    {
        public static async Task<FileAttribute> ResolveSymlinkTarget(string symlinkTarget, List<ProviderInfo> providerInfos, CancellationToken token)
        {
            if (string.IsNullOrEmpty(symlinkTarget))
            {
                throw new ArgumentException("Symlink target cannot be null or empty", nameof(symlinkTarget));
            }
                int spaceIdTokenStart = symlinkTarget.IndexOf('<');
                int spaceIdTokenEnd = symlinkTarget.IndexOf('>');
                string? spaceId = spaceIdTokenStart >= 0 && spaceIdTokenEnd > spaceIdTokenStart
                    ? symlinkTarget.Substring(spaceIdTokenStart + 1, spaceIdTokenEnd - spaceIdTokenStart - 1)
                    : null;
                
                if (spaceId == null)
                {
                    throw new ArgumentException("Symlink target does not contain a valid space ID", nameof(symlinkTarget));
                }

                string pathInSpace = spaceIdTokenEnd >= 0
                    ? symlinkTarget.Substring(spaceIdTokenEnd + 1)
                    : symlinkTarget;

                FileAttribute space = await RestClient.GetFileAttribute(spaceId, providerInfos, token);
                string targetPath = "/" + space.name + pathInSpace;

                FileId targetFile = await RestClient.LookupFileId(providerInfos, targetPath, token);
                FileAttribute target = await RestClient.GetFileAttribute(targetFile.fileId, providerInfos, token);
                if (target.fileType == FileTypeOD.SYMLNK)
                {
                    return await ResolveSymlinkTarget(target.symlinkValue, providerInfos, token);
                }
                else
                {
                    return target;
                }
        }
    }
}