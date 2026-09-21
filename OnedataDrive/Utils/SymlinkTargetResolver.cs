using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;

namespace OnedataDrive.Utils
{
    public static class SymlinkTargetResolver
    {
        public static FileAttribute ResolveSymlinkTarget(string symlinkTarget, List<ProviderInfo> providerInfos, CancellationToken token, out string resolvedTargetPath)
        {
            try
            {
                resolvedTargetPath = string.Empty;

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
                    throw new ArgumentException($"Symlink target does not contain a valid space ID. Actual: {symlinkTarget}", nameof(symlinkTarget));
                }

                // The token may carry a "__onedata_space_id:" prefix - the space ID itself follows it
                const string SPACE_ID_PREFIX = "__onedata_space_id:";
                if (spaceId.StartsWith(SPACE_ID_PREFIX))
                {
                    spaceId = spaceId.Substring(SPACE_ID_PREFIX.Length);
                }

                string pathInSpace = spaceIdTokenEnd >= 0
                    ? symlinkTarget.Substring(spaceIdTokenEnd + 1)
                    : symlinkTarget;

                FileAttribute space = RestClient.GetFileAttribute(spaceId, providerInfos, token).Result;
                string targetPath = "/" + space.name + pathInSpace;

                FileId targetFile = RestClient.LookupFileId(providerInfos, targetPath, token).Result;
                FileAttribute target = RestClient.GetFileAttribute(targetFile.fileId, providerInfos, token).Result;
                if (target.fileType == FileTypeOD.SYMLNK)
                {
                    return ResolveSymlinkTarget(target.symlinkValue, providerInfos, token, out resolvedTargetPath);
                }
                else
                {
                    resolvedTargetPath = symlinkTarget;
                    return target;
                }
            }
            catch (AggregateException e) when (e.InnerException is NoSuchCloudFile)
            {
                throw new CanNotResolveSymlinkTarget($"Failed to resolve symlink target: {symlinkTarget}", e);
            }
        }
    }

    public class CanNotResolveSymlinkTarget : Exception
    {
        public CanNotResolveSymlinkTarget(string message) : base(message) { }
        public CanNotResolveSymlinkTarget(string message, Exception innerException) : base(message, innerException) { }
    }
}