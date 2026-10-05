using System.Runtime.InteropServices;
using System.Text;
using OnedataDrive.Utils;

namespace OnedataDrive
{
    public struct FileIdentity
    {
        // Serialized layout:
        // [fileType:4][fileID length:4][fileID:N][symlinkTarget length:4][symlinkTarget:M]
        // The trailing symlinkTarget section is only written when symlinkTarget is not null, so
        // identities without a symlink target keep the original layout byte for byte.
        public const uint HEADER_SIZE = (uint)(sizeof(int) + sizeof(int));

        public string fileID { get; set; }
        public FileTypeOD fileType { get; set; }
        public string? symlinkTargetPath { get; set; }

        public FileIdentity()
        {
            this.fileType = FileTypeOD.EMPTY;
            this.fileID = "";
            this.symlinkTargetPath = null;
        }

        public FileIdentity(string fileID, FileTypeOD fileType, string? symlinkTargetId = null)
        {
            this.fileID = fileID;
            this.fileType = fileType;
            this.symlinkTargetPath = symlinkTargetId;
        }

        /// <summary>
        /// Converts FileIdentity to unmanaged memory with layout:
        /// [fileType size][4 bytes: fileID length][N bytes: UTF-8 fileID]
        /// [4 bytes: symlinkTarget length][M bytes: UTF-8 symlinkTarget]
        /// The symlinkTarget section is written only when symlinkTarget is not null.
        /// </summary>
        public UnmanagedMem ToUnmanagedMemory()
        {
            byte[] fileIDBytes = Encoding.UTF8.GetBytes(fileID ?? "");
            uint stringLength = (uint)fileIDBytes.Length;

            // symlinkTarget == null means "no target at all", empty string means "target is empty"
            byte[] symlinkTargetBytes = symlinkTargetPath != null ? Encoding.UTF8.GetBytes(symlinkTargetPath) : [];
            uint symlinkTargetLength = symlinkTargetPath != null ? (uint)symlinkTargetBytes.Length : 0;
            bool writeSymlinkTarget = symlinkTargetPath != null;

            // Layout: sizeof(FileTypeOD) + sizeof(int) + N (fileID) [+ sizeof(int) + M (symlinkTarget)]
            uint totalSize = (uint)sizeof(FileTypeOD) + sizeof(int) + stringLength;
            if (writeSymlinkTarget)
            {
                totalSize += (uint)sizeof(int) + symlinkTargetLength;
            }

            UnmanagedMem unmanagedMem = new(totalSize);
            nint ptr = unmanagedMem.GetPointer();
            int offset = 0;

            // Write fileType
            Marshal.WriteInt32(ptr, (int)fileType);
            offset += sizeof(int);

            // Write string length (4 bytes)
            Marshal.WriteInt32(ptr + offset, (int)stringLength);
            offset += sizeof(int);

            // Write string data
            if (stringLength > 0)
            {
                Marshal.Copy(fileIDBytes, 0, ptr + offset, (int)stringLength);
                offset += (int)stringLength;
            }

            // Write symlink target
            if (writeSymlinkTarget)
            {
                Marshal.WriteInt32(ptr + offset, (int)symlinkTargetLength);
                offset += sizeof(int);

                if (symlinkTargetLength > 0)
                {
                    Marshal.Copy(symlinkTargetBytes, 0, ptr + offset, (int)symlinkTargetLength);
                }
            }

            return unmanagedMem;
        }

        /// <summary>
        /// Reconstructs FileIdentity from unmanaged memory
        /// </summary>
        /// 

        public static FileIdentity FromUnmanagedMemory(byte[] managedMem, uint size)
        {
            GCHandle handle = GCHandle.Alloc(managedMem, GCHandleType.Pinned);
            try
            {
                return FromUnmanagedMemory(handle.AddrOfPinnedObject(), size);
            }
            finally
            {
                handle.Free();
            }
        }

        public static FileIdentity FromUnmanagedMemory(UnmanagedMem unmanagedMem)
        {
            return FromUnmanagedMemory(unmanagedMem.GetPointer(), unmanagedMem.GetSize());
        }

        public static FileIdentity FromUnmanagedMemory(nint ptr, uint size)
        {
            if (ptr == nint.Zero)
            {
                throw new ArgumentException("Invalid unmanaged memory pointer (null)");
            }

            if (size < sizeof(FileTypeOD) + sizeof(int))
            {
                throw new ArgumentException("Invalid unmanaged memory size (too short)");
            }


            int offset = 0;

            // Read fileType
            int fileTypeValue = Marshal.ReadInt32(ptr);
            offset += sizeof(int);
            FileTypeOD fileType = (FileTypeOD)fileTypeValue;

            // Read fileID length (4 bytes)
            int stringLength = Marshal.ReadInt32(ptr + offset);
            offset += sizeof(int);

            // Read fileID data
            string fileID = "";
            if (stringLength > 0)
            {
                byte[] buffer = new byte[stringLength];
                Marshal.Copy(ptr + offset, buffer, 0, stringLength);
                fileID = Encoding.UTF8.GetString(buffer);
                offset += stringLength;
            }

            // Read symlink target - absent in identities written before symlink targets were stored
            string? symlinkTarget = null;
            if (offset + sizeof(int) <= size)
            {
                int symlinkTargetLength = Marshal.ReadInt32(ptr + offset);
                offset += sizeof(int);

                if (symlinkTargetLength > 0 && offset + symlinkTargetLength <= size)
                {
                    byte[] buffer = new byte[symlinkTargetLength];
                    Marshal.Copy(ptr + offset, buffer, 0, symlinkTargetLength);
                    symlinkTarget = Encoding.UTF8.GetString(buffer);
                }
                else
                {
                    symlinkTarget = "";
                }
            }

            return new FileIdentity(fileID, fileType, symlinkTarget);
        }
    }
}
