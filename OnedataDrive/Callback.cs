using System.Runtime.InteropServices;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive
{
    public class Callback
    {
        public CF_CONNECTION_KEY connectionKey;
        public CF_TRANSFER_KEY transferKey;
        public long windowsFileId;
        public FileIdentity fileIdentity;
        public uint fileIdentityLength;
        public string volumeDosName;
        public string normalizedPath;
        public long fileSize;
        public string filePath;
        public Callback(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            this.windowsFileId = callbackInfo.FileId;
            this.connectionKey = callbackInfo.ConnectionKey;
            this.transferKey = callbackInfo.TransferKey;
            this.fileIdentityLength = callbackInfo.FileIdentityLength;
            if (this.fileIdentityLength > 0)
            {
                this.fileIdentity = FileIdentity.FromUnmanagedMemory(callbackInfo.FileIdentity, callbackInfo.FileIdentityLength);
            }
            else
            {
                this.fileIdentity = new FileIdentity();
            }
            this.volumeDosName = callbackInfo.VolumeDosName;
            this.normalizedPath = callbackInfo.NormalizedPath;
            this.fileSize = callbackInfo.FileSize;
            this.filePath = this.volumeDosName + this.normalizedPath;
        }
    }
}
