using System.Runtime.InteropServices;
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

        public UnmanagedMem GetUnmanagedMemory()
        {
            UnmanagedMem unmanagedMem = new((uint)Marshal.SizeOf(this));
            Marshal.StructureToPtr(this, unmanagedMem.GetPointer(), false);
            return unmanagedMem;
        }
    }
}
