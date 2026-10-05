using System.Runtime.InteropServices;

namespace OnedataDrive.Utils
{
    public class UnmanagedMem : IDisposable
    {
        private nint pointer = IntPtr.Zero;
        private uint size = 0;

        public UnmanagedMem(uint size)
        {
            _Allocate(size);
        }

        private void _Allocate(uint size)
        {
            try
            {
                this.pointer = Marshal.AllocCoTaskMem((int)size);
            }
            catch (Exception e) 
            {
                throw new UnmanagedMemoryException("Failed to allocate unmanaged memory - Marshal exeption", e);
            }
            
            if (pointer == IntPtr.Zero)
            {
                throw new UnmanagedMemoryException("Failed to allocate unmanaged memory.");
            }
            this.size = size;
        }

        public void Realloc(uint size)
        {
            if (pointer == IntPtr.Zero)
            {
                throw new UnmanagedMemoryException("Unmanaged memory is not allocated.");
            }
            nint newPointer = IntPtr.Zero;

            try
            {
                newPointer = Marshal.ReAllocCoTaskMem(this.pointer, (int)size);
            }
            catch (Exception e)
            {
                throw new UnmanagedMemoryException("Failed to reallocate unmanaged memory - Marshal exception", e);
            }

            
            if (newPointer == IntPtr.Zero)
            {
                throw new UnmanagedMemoryException("Failed to reallocate unmanaged memory.");
            }
            this.pointer = newPointer;
            this.size = size;
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

        public uint GetSize()
        {
            return size;
        }

        public void Dispose()
        {
            if (this.pointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(this.pointer);
                this.pointer = IntPtr.Zero;
                this.size = 0;
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
