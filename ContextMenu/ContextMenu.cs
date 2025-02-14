using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OnedataDrive;
using OnedataDrive.Utils;

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
        protected string pipeName = "testpipe";
        protected override bool CanShowMenu()
        {
            try
            {
                string? path = GetRootPath();
                if (path != null)
                {
                    return SelectedItemPaths.All(x => x.StartsWith(path));
                }
                return false;
            }
            catch
            {
                return false;
            }
            
        }

        protected override ContextMenuStrip CreateMenu()
        {
            var menu = new ContextMenuStrip();

            var mainItem = new ToolStripMenuItem
            {
                Text = "OnedataDrive"
            };

            var refreshFolder = new ToolStripMenuItem
            {
                Text = "Refresh Folder"
            };
            var refreshSpace = new ToolStripMenuItem
            {
                Text = "Refresh Space"
            };
            refreshFolder.Click += RequestRefresh;
            refreshSpace.Click += RequestRefresh;

            mainItem.DropDownItems.Add(refreshFolder);
            mainItem.DropDownItems.Add(refreshSpace);
            menu.Items.Add(mainItem);

            return menu;
        }

        private string? GetRootPath()
        {
            NamedPipeClientStream client = new(".", pipeName, PipeDirection.InOut);
            try
            {
                string? rootPath = null;
                client.Connect(100);
                if (client.IsConnected)
                {
                    StreamWriter writer = new(client);
                    StreamReader reader = new(client);
                    writer.WriteLine(new PipeCommand(Commands.SEND_ROOT).ToString());
                    writer.Flush();
                    CancellationTokenSource tokenSource = new CancellationTokenSource();
                    ValueTask<string?> readerTask = reader.ReadLineAsync(tokenSource.Token);
                    tokenSource.CancelAfter(120);
                    for (int i = 0; i < 4; i++)
                    {
                        Task.Delay(30).Wait();
                        if (readerTask.IsCompletedSuccessfully)
                        {
                            string rawMsg = readerTask.Result ?? "";
                            PipeCommand command = new(rawMsg);
                            if (command.command == Commands.OK)
                            {
                                rootPath = command.payload[0];
                            }
                            break;
                        }
                    }          
                }
                return rootPath;
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (client.IsConnected)
                {
                    client.Close();
                    client.Dispose();
                }
            }
        }

        private void RequestRefresh(object? sender, EventArgs e)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            Task.Run(() => 
            {
                using (NamedPipeClientStream client = new(".", pipeName, PipeDirection.InOut))
                {
                    client.Connect(100);
                    if (client.IsConnected)
                    {
                        StreamWriter writer = new(client);
                        StreamReader reader = new(client);
                        writer.WriteLine(new PipeCommand(Commands.REQUEST_REFRESH, SelectedItemPaths.ToList()).ToString());
                        writer.Flush();

                        ValueTask<string?> readerTask = reader.ReadLineAsync(tokenSource.Token);

                        for (int i = 0; i < 4; i++)
                        {
                            Task.Delay(100).Wait();
                            if (readerTask.IsCompletedSuccessfully)
                            {
                                PipeCommand command = new(readerTask.Result ?? "");
                                if (command.command == Commands.OK)
                                {
                                    MessageBox.Show("Refresh Started");
                                }
                                else
                                {
                                    MessageBox.Show("Refresh Did NOT start");
                                }
                                break;
                            }
                            if (readerTask.IsFaulted)
                            {
                                break;
                            }
                        }
                        if (!readerTask.IsCompleted)
                        {
                            tokenSource.Cancel();
                        }
                    }

                }
            }, tokenSource.Token);           
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
}
