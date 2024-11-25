using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.ApplicationModel.Background;
using static Vanara.PInvoke.Ole32;

namespace OnedataDrive.CloudSync
{
    internal class ClassFactory<T> : IClassFactory
    {
        public HRESULT CreateInstance(object? pUnkOuter, in Guid riid, out object? ppvObject)
        {
            // NOT IMPLEMENTED
            ppvObject = null;
            return HRESULT.S_OK;
        }

        public HRESULT LockServer(bool fLock)
        {
            return HRESULT.S_OK;
        }
    }
}
