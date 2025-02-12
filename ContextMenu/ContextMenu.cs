using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ContextMenu
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [COMServerAssociation(AssociationType.AllFilesAndFolders)]
    [COMServerAssociation(AssociationType.Directory)]
    [COMServerAssociation(AssociationType.DirectoryBackground)]
    [Guid("B3B9C5B6-5C92-4D1B-85E6-2F80B72F6E28")]
    public class SimpleContextMenu : SharpContextMenu
    {
        protected override bool CanShowMenu()
        {
            try
            {
                string pipeName = "testpipe";
                bool pipeExists = Directory.GetFiles(@"\\.\pipe\").Contains($"\\\\.\\pipe\\{pipeName}", StringComparer.OrdinalIgnoreCase);
                if (pipeExists)
                {
                    Debug.Print("PIPE EXISTS");
                    if (SelectedItemPaths.ToList<string>().Any(s => s.StartsWith("C:\\Users\\User\\OnedataDrive")))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                
            }
            catch
            {
                return true;
            }
            
        }

        protected override ContextMenuStrip CreateMenu()
        {
            var menu = new ContextMenuStrip();

            var mainItem = new ToolStripMenuItem
            {
                Text = "Simple Extension Action"
            };

            var actionItem = new ToolStripMenuItem
            {
                Text = "Perform Action"
            };
            //actionItem.Click += (sender, e) => MessageBox.Show("Action performed!");

            mainItem.DropDownItems.Add(actionItem);
            menu.Items.Add(mainItem);

            return menu;
        }

        [ComRegisterFunction]
        public static void Register(Type t)
        {
            RegisterContextMenu(t, @"*\shellex\ContextMenuHandlers\");
            RegisterContextMenu(t, @"Directory\shellex\ContextMenuHandlers\");
            RegisterContextMenu(t, @"Directory\Background\shellex\ContextMenuHandlers\");
        }

        private static void RegisterContextMenu(Type t, string basePath)
        {
            string keyPath = basePath + "SimpleContextMenu";
            using (var key = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(keyPath))
            {
                if (key == null)
                {
                    Console.WriteLine($"Failed to create registry key: {keyPath}");
                }
                else
                {
                    key.SetValue(null, t.GUID.ToString("B"));
                }
            }
        }

        [ComUnregisterFunction]
        public static void Unregister(Type t)
        {
            UnregisterContextMenu(@"*\shellex\ContextMenuHandlers\");
            UnregisterContextMenu(@"Directory\shellex\ContextMenuHandlers\");
            UnregisterContextMenu(@"Directory\Background\shellex\ContextMenuHandlers\");
        }

        private static void UnregisterContextMenu(string basePath)
        {
            string keyPath = basePath + "SimpleContextMenu";
            try
            {
                Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(keyPath, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unregistering COM server from {keyPath}: {ex.Message}");
            }
        }
    }

    private NamedPipe ConnecteToPipe()
    {

    }

}
