using System.Runtime.InteropServices;

namespace OnedataDrive.Utils
{
    public class UnmanagedMem : IDisposable
    {
        private nint pointer = IntPtr.Zero;
        public UnmanagedMem(uint size)
        {
            this.pointer = Marshal.AllocCoTaskMem((int)size);
            if (pointer == IntPtr.Zero)
            {
                throw new UnmanagedMemoryException("Failed to allocate unmanaged memory.");
            }
        }

        public nint GetPointer()
        {
            if (pointer == IntPtr.Zero)
            {
                throw new UnmanagedMemoryException("Unmanaged memory pointer is null.");
            }
            return pointer;
        }

        public bool IsValid()
        {
            return pointer != IntPtr.Zero;
        }

        public void Dispose()
        {
            if (this.pointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(this.pointer);
                this.pointer = IntPtr.Zero;
            }
        }
    }

    public class UnmanagedMemoryException : Exception
    {
        public UnmanagedMemoryException() : base() { }
        public UnmanagedMemoryException(string message) : base(message) { }
        public UnmanagedMemoryException(string message, Exception inner) : base(message, inner) { }
    }
}
