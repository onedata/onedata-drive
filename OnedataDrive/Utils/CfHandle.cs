using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.Ole32;

namespace OnedataDrive.Utils
{
    public class CfHandle : IDisposable
    {
        public const uint FILE_NOT_FOUND = 0x80070002u; //-2147024894

        private SafeHCFFILE? handle = null;
        public string path { get; private set; } = "";
        public string id = IdGenerator.GenerateId8();
        
        public CfHandle(string path, CF_OPEN_FILE_FLAGS flags)
        {
            HRESULT hres = CfOpenFileWithOplock(path, flags, out SafeHCFFILE handle);

            if (hres == FILE_NOT_FOUND)
            {
                FileNotFoundHandleException innerEx = new FileNotFoundHandleException($"Path: {path}", hresult: hres);
                throw new FileNotFoundException($"File not found: {path}", innerEx);
            }
            else if (hres.Failed)
            {
                throw new FileHandleException($"Failed to open file handle for {path}.", hresult: hres);
            }

            this.path = path;
            this.handle = handle;
            CfHandleList.handles.Add(this);
        }

        public void Dispose()
        {
            if (handle != null && !handle.IsInvalid)
            {
                handle.Dispose();
            }
            handle = null;

            CfHandleList.handles.Remove(this);
        }

        public SafeHCFFILE GetSafeHandle()
        {
            if (handle == null || handle.IsInvalid)
            {
                throw new FileHandleException("File handle is not valid.");
            }
            return handle;
        }

        public nint GetDangerousHandle()
        {
            if (handle == null || handle.IsInvalid)
            {
                throw new FileHandleException("File handle is not valid.");
            }
            return GetSafeHandle().DangerousGetHandle();
        }

        public bool IsValid()
        {
            return handle != null && !handle.IsInvalid;
        }
    }

    public static class CfHandleList
    {
        public static List<CfHandle> handles = new List<CfHandle>();
    }

    public class  FileHandleException : Exception
    {
        public HRESULT? hresult { get; protected set; } = null;
        public FileHandleException(HRESULT? hresult = null) 
            : base() 
        {
            this.hresult = hresult;
        }
        public FileHandleException(string message, HRESULT? hresult = null) 
            : base(message) 
        {
            this.hresult = hresult;
        }
        public FileHandleException(string message, Exception innerException, HRESULT? hresult = null) 
            : base(message, innerException) 
        {
            this.hresult = hresult;
        }
        public override string ToString()
        {
            return $"FileHandleException, {HRESULT()} : " + base.ToString();
        }

        protected string HRESULT()
        {
            if (hresult.HasValue)
            {
                return $"HRESULT 0x{hresult:X} -> {hresult}";
            }
            else
            {
                return "HRESULT: not set";
            }
        }
    }

    public class FileNotFoundHandleException : FileHandleException
    {
        public FileNotFoundHandleException(HRESULT? hresult = null) 
            : base(hresult: hresult) { }
        public FileNotFoundHandleException(string message, HRESULT? hresult = null) 
            : base(message, hresult: hresult) { }
        public FileNotFoundHandleException(string message, Exception innerException, HRESULT? hresult = null) 
            : base(message, innerException, hresult: hresult) { }
        public override string ToString()
        {
            return $"FileNotFoundHandleException, {HRESULT()} : " + base.ToString();
        }
    }
}
