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
    [Guid("9D369C15-EDCD-4916-B8B9-0BBD1666A47F")]
    public class ContextMenu : SharpContextMenu
    {
        protected string pipeName = CloudSync.PIPE_SERVER_NAME;
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

            var refreshFiles = new ToolStripMenuItem
            {
                Text = "Refresh Selected Items"
            };
            var refreshFolder = new ToolStripMenuItem
            {
                Text = "Refresh Current Folder"
            };
            var refreshSpace = new ToolStripMenuItem
            {
                Text = "Refresh Space"
            };
            refreshFiles.Click += RefreshFiles;
            refreshFolder.Click += RefreshFolder;
            refreshSpace.Click += RefreshSpace;

            mainItem.DropDownItems.Add(refreshFiles);
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
                client.Connect(200);
                if (client.IsConnected)
                {
                    StreamWriter writer = new(client);
                    StreamReader reader = new(client);
                    writer.WriteLine(new PipeCommand(Commands.SEND_ROOT).ToString());
                    writer.Flush();
                    CancellationTokenSource tokenSource = new CancellationTokenSource();
                    ValueTask<string?> readerTask = reader.ReadLineAsync(tokenSource.Token);
                    for (int i = 0; i < 5; i++)
                    {
                        Task.Delay(50).Wait();
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
                    tokenSource.Cancel();
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

        private void RefreshFiles(object? sender, EventArgs e)
        {
            PipeCommand command = new(Commands.REFRESH_FILES, SelectedItemPaths.ToList());
            SendCommand(command);
        }

        private void RefreshFolder(object? sender, EventArgs e)
        {
            PipeCommand command = new(Commands.REFRESH_FOLDER, SelectedItemPaths.ToList());
            SendCommand(command);
        }

        private void RefreshSpace(object? sender, EventArgs e)
        {
            PipeCommand command = new(Commands.REFRESH_SPACE, SelectedItemPaths.ToList());
            SendCommand(command);
        }

        private void SendCommand(PipeCommand commandToSend)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            Task.Run(() => 
            {
                using (NamedPipeClientStream client = new(".", pipeName, PipeDirection.InOut))
                {
                    client.Connect(300);
                    if (client.IsConnected)
                    {
                        StreamWriter writer = new(client);
                        StreamReader reader = new(client);
                        writer.WriteLine(commandToSend.ToString());
                        writer.Flush();

                        ValueTask<string?> readerTask = reader.ReadLineAsync(tokenSource.Token);

                        for (int i = 0; i < 10; i++)
                        {
                            Task.Delay(100).Wait();
                            if (readerTask.IsCompletedSuccessfully)
                            {
                                PipeCommand commandReceived = new(readerTask.Result ?? "");
                                if (commandReceived.command == Commands.OK)
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
            string keyPath = basePath + "ContextMenu";
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
            string keyPath = basePath + "ContextMenu";
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
