using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.Ole32;

namespace OnedataDrive.CloudSync
{
    internal class RefreshContextMenu : IExplorerCommand, IObjectWithSite
    {
        private object? _site = null;

        public HRESULT GetTitle(IShellItemArray psiItemArray, out string? ppszName)
        {
            ppszName = "Refresh from server";
            return HRESULT.S_OK;
        }

        public HRESULT GetIcon(IShellItemArray psiItemArray, out string? ppszIcon)
        {
            ppszIcon = null;
            return HRESULT.E_NOTIMPL;
        }

        public HRESULT GetToolTip(IShellItemArray psiItemArray, out string? ppszInfotip)
        {
            ppszInfotip = null;
            return HRESULT.E_NOTIMPL;
        }

        public HRESULT GetCanonicalName(out Guid pguidCommandName)
        {
            pguidCommandName = Guid.Empty;
            return HRESULT.E_NOTIMPL;
        }

        public HRESULT GetState(IShellItemArray psiItemArray, bool fOkToBeSlow, out EXPCMDSTATE pCmdState)
        {
            pCmdState = EXPCMDSTATE.ECS_ENABLED;
            return HRESULT.S_OK;
        }

        public HRESULT Invoke(IShellItemArray psiItemArray, IBindCtx? pbc)
        {
            Debug.Print("RefreshContextMenu command received");

            IEnumShellItems items = psiItemArray.EnumItems();
            for (uint i = 0; i < psiItemArray.GetCount(); i++)
            {
                IShellItem item = psiItemArray.GetItemAt(i);
                Debug.Print("Item no {0}: {1}", i + 1, item.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY));
            }

            Debug.Print("DONE");
            return HRESULT.S_OK;
        }

        public HRESULT GetFlags(out EXPCMDFLAGS pFlags)
        {
            pFlags = EXPCMDFLAGS.ECF_DEFAULT;
            return HRESULT.S_OK;
        }

        public HRESULT EnumSubCommands(out IEnumExplorerCommand? ppEnum)
        {
            ppEnum = null;
            return HRESULT.E_NOTIMPL;
        }

        public HRESULT SetSite(object? pUnkSite)
        {
            _site = pUnkSite;
            return HRESULT.S_OK;
        }

        public HRESULT GetSite(in Guid riid, out object? ppvSite)
        {
            ppvSite = _site;
            return HRESULT.S_OK;
        }
    }
}
