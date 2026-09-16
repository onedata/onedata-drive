using System.Runtime.InteropServices;
using System.Text;
using OnedataDrive.Utils;

namespace OnedataDrive
{
    public struct FileIdentity
    {
        public string fileID { get; set; }
        public FileTypeOD fileType { get; set; }

        public FileIdentity()
        {
            this.fileType = FileTypeOD.EMPTY;
            this.fileID = "";
        }

        public FileIdentity(string fileID, FileTypeOD fileType)
        {
            this.fileID = fileID;
            this.fileType = fileType;
        }

        /// <summary>
        /// Converts FileIdentity to unmanaged memory with layout:
        /// [fileType size][4 bytes: string length][N bytes: UTF-8 string data]
        /// </summary>
        public UnmanagedMem ToUnmanagedMemory()
        {
            byte[] fileIDBytes = Encoding.UTF8.GetBytes(fileID ?? "");
            uint stringLength = (uint)fileIDBytes.Length;

            // Layout: sizeof(FileTypeOD) + sizeof(int) + N (string data)
            uint totalSize = (uint)sizeof(FileTypeOD) + sizeof(int) + stringLength;

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

            // Read string length (4 bytes)
            int stringLength = Marshal.ReadInt32(ptr + offset);
            offset += sizeof(int);

            // Read string data
            string fileID = "";
            if (stringLength > 0)
            {
                byte[] buffer = new byte[stringLength];
                Marshal.Copy(ptr + offset, buffer, 0, stringLength);
                fileID = Encoding.UTF8.GetString(buffer);
            }

            return new FileIdentity(fileID, fileType);
        }
    }
}
