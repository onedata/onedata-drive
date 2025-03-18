using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Reflection.Metadata;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive
{
    public class PipeServer
    {
        public bool running { get; private set; } = false;
        public string pipeName { get; private set; } = "";
        private CancellationTokenSource cts = new();
        private Task? serverTask;
        
        public void Start(string pipeName)
        {
            if (running)
            {
                return;
            }
            cts = new();
            this.pipeName = pipeName;
            this.serverTask = Task.Run(() => StartServer(cts.Token), cts.Token);
            this.running = true;
            Debug.Print("PIPE SERVER: running");
        }

        public void Stop()
        {
            if (!running)
            {
                return;
            }
            cts.Cancel();
            if (this.serverTask is not null)
            {
                this.serverTask.Wait();
            }
            this.running = false;
            Debug.Print("PIPE SERVER: stopped");
        }

        public async void StopAsync()
        {
            await Task.Run(() => Stop());
        }

        private void StartServer(CancellationToken cToken)
        {
            while (true)
            {
                try
                {
                    using (NamedPipeServerStream server = new(pipeName, PipeDirection.InOut))
                    {
                        StreamReader reader = new StreamReader(server);
                        StreamWriter writer = new StreamWriter(server);
                        Debug.Print("PIPE SERVER: ready for client");
                        Task waitForConnection = server.WaitForConnectionAsync(cToken);
                        while (!waitForConnection.IsCompleted)
                        {
                            if (cToken.IsCancellationRequested)
                            {
                                server.Close();
                                return;
                            }
                            Task.Delay(10).Wait();
                        }

                        Debug.Print("PIPE SERVER: listening for client`s messages");
                        ValueTask<string?> readerTask = reader.ReadLineAsync(cToken);
                        while (server.IsConnected)
                        {
                            if (cToken.IsCancellationRequested)
                            {
                                server.Disconnect();
                                server.Close();
                                return;
                            }
                            if (!readerTask.IsCompleted)
                            {
                                Task.Delay(100).Wait();
                                continue;
                            }
                            else
                            {
                                string received = readerTask.Result ?? "";
                                Debug.Print("PIPE SERVER received: " + received);
                                string response = HandleCommand(received);
                                writer.WriteLine(response);
                                writer.FlushAsync().Wait();
                                readerTask = reader.ReadLineAsync(cToken);
                            }
                        }
                        Debug.Print("PIPE SERVER: client disconnected");
                        if (cToken.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("PIPE SERVER FAILED: " + ex.ToString());
                    if (cToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
        }

        private string HandleCommand(string msg)
        {
            PipeCommand received = new(msg ?? "");

            string response = "";

            switch (received.command)
            {
                case Commands.SEND_ROOT:
                    Debug.Print("Send root");
                    response = new PipeCommand(Commands.OK, [CloudSync.configuration.root_path]).ToString();
                    break;
                case Commands.REFRESH_SPACE:
                    // do something
                    Debug.Print("Refresh space");
                    received.payload.ForEach(x => Debug.Print($"Path: {x}"));
                    response = new PipeCommand(Commands.FAIL).ToString();
                    break;
                case Commands.REFRESH_FOLDER_DOWN:
                    Debug.Print("Refresh folder down");
                    received.payload.ForEach(x => Debug.Print($"Path: {x}"));
                    response = new PipeCommand(Commands.FAIL).ToString();
                    break;
                case Commands.REFRESH_FOLDER:
                    Debug.Print("Refresh folder");
                    received.payload.ForEach(x => Debug.Print($"Path: {x}"));
                    RefreshFolder(received.payload[0]);
                    response = new PipeCommand(Commands.OK).ToString();
                    break;
                default:
                    response = new PipeCommand(Commands.FAIL).ToString();
                    Debug.Print("Default");
                    break;
            }
            Debug.Print("PIPE SERVER msg received");
            return response;
        }

        private void RefreshFolder(string folderPath)
        {
            Debug.Print("Is space path: " + PathUtils.IsSpacePath(folderPath));
            Debug.Print("Is isDirectory: " + Directory.Exists(folderPath));
            if (Directory.Exists(folderPath) && PathUtils.IsSpacePath(folderPath))
            {
                Debug.Print("REFRESH START");
                // get folder path id
                CF_PLACEHOLDER_BASIC_INFO info = CldApiUtils.GetBasicInfo(folderPath);
                string dirId = System.Text.Encoding.Unicode.GetString(info.FileIdentity);

                // based on id get contents info from server
                SpaceFolder sf = CloudSync.spaces[PathUtils.GetSpaceName(folderPath)];
                Task<DirChildren> dirChildrenTask = RestClient.GetFilesAndSubdirs(dirId, sf.providerInfos);

                // get local info on all files/folders
                List<(CF_PLACEHOLDER_BASIC_INFO info, string path)> localInfos = new();
                Task localDirChildrenTask = Task.Run(() =>
                {
                    foreach (string path in Directory.GetDirectories(folderPath))
                    {
                        localInfos.Add((CldApiUtils.GetBasicInfo(path), path));
                    }
                    foreach (string path in Directory.GetFiles(folderPath))
                    {
                        localInfos.Add((CldApiUtils.GetBasicInfo(path), path));
                    }
                });
                

                // get result from cloud and local
                DirChildren dirChildren = dirChildrenTask.Result;
                List<(Child child, bool visited)> cloudInfos = dirChildren.children.ConvertAll(x => (x, false));
                localDirChildrenTask.Wait();

                NameConvertor nameConvertor = new NameConvertor();

                // compare them
                foreach ((CF_PLACEHOLDER_BASIC_INFO localInfo, string localPath) in localInfos)
                {
                    string localId = System.Text.Encoding.Unicode.GetString(localInfo.FileIdentity);
                    int index = cloudInfos.FindIndex(item => item.child.file_id == localId);
                    if (index < 0)
                    {
                        // delete local file/dir
                        Debug.Print("Placeholder delete needed: " + localPath);
                        try
                        {
                            if (File.Exists(localPath))
                            {
                                File.Delete(localPath);
                            }
                            else
                            {
                                Directory.Delete(localPath, true);
                            }
                            Debug.Print("File delete OK");
                        }
                        catch (Exception e)
                        {
                            Debug.Print("FAILED to remove file: " + e);
                        }
                    }
                    else
                    {
                        CloudSync.watcher.Pause();
                        try
                        {
                            cloudInfos[index] = (cloudInfos[index].child, true);
                            Child cloudInfo = cloudInfos[index].child;
                            // compare size


                            if (cloudInfo.type == "REG")
                            {
                                FileInfo localFileInfo = new(localPath);
                                if (cloudInfo.size != localFileInfo.Length)
                                {
                                    // update placeholder
                                    Debug.Print("Placeholder update needed: " + localPath);
                                    UpdatePlaceholder(localPath, cloudInfo);
                                }
                            }

                            // compare name
                            string cloudWindowsName = nameConvertor.MakeWindowsCorrect(cloudInfo.name, out bool idAttached, cloudInfo.file_id);
                            string localName = PathUtils.GetLastInPath(localPath);
                            if (localName != cloudWindowsName)
                            {
                                RenamePlaceholder(localPath, cloudWindowsName, idAttached, cloudInfo.file_id);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.Print("FAILED to update placeholder: " + e);
                        }
                        CloudSync.watcher.Resume();
                    }
                }
                List<Child> newChildren = cloudInfos.Where(x => x.visited == false).Select(x => x.child).ToList();
                PlaceholderCreateInfo createInfo = new PlaceholderCreateInfo();
                
                foreach (Child cloudInfo in newChildren)
                {
                    Debug.Print("NEW PLACEHOLDER: " + cloudInfo.name);
                    string windowsCorrectName = nameConvertor.MakeWindowsCorrectDistinct(cloudInfo.name, cloudInfo.file_id, createInfo);
                    PlaceholderData placeholderData = new(
                        cloudInfo.file_id,
                        windowsCorrectName,
                        cloudInfo.size,
                        cloudInfo.atime,
                        cloudInfo.mtime,
                        cloudInfo.ctime
                        );
                    if (cloudInfo.type == "REG")
                    {
                        createInfo.Add(Placeholders.createInfo(placeholderData));
                    }
                    else if (cloudInfo.type == "DIR")
                    {
                        createInfo.Add(Placeholders.createDirInfo(placeholderData));
                    }
                    
                }
                CloudSync.CreatePlaceholders(createInfo, folderPath);
                Debug.Print("REFRESH FINISHED");
            }

        }

        private void RenamePlaceholder(string localPath, string windowsCorrectName, bool idAttached, string id)
        {
            string parentPath = PathUtils.GetParentPath(localPath);
            string suffix = "";
            for (int i = 1; i < 1000; i++)
            {
                string newName = windowsCorrectName + suffix;
                try
                {
                    string newPath = Path.Join(parentPath, newName);
                    Rename(localPath, newPath);
                    Debug.Print($"Renamed {newPath}");
                    CldApiUtils.SetInSyncState(newPath);
                    break;
                }
                catch (IOException e)
                {
                    Debug.Print("Rename FAIL: " + e);
                }

                if (!idAttached)
                {
                    windowsCorrectName += NameConvertor.IdSuffix(id);
                    idAttached = true;
                }
                else
                {
                    if (i == 1)
                    {
                        i = 2;
                    }
                    suffix = $"({i})";
                }
            }
        }

        private void Rename(string oldPath, string newPath)
        {
            if (Directory.Exists(oldPath))
            {
                    Directory.Move(oldPath, newPath);
            }
            else if (File.Exists(oldPath))
            {
                    File.Move(oldPath, newPath);
            }
        }
          

        private void UpdatePlaceholder(string placeholderPath, Child cloudInfo, bool isDirectory = false)
        {
            PlaceholderData placeholderData = new(
                cloudInfo.file_id, 
                cloudInfo.name, 
                cloudInfo.size, 
                cloudInfo.atime, 
                cloudInfo.mtime, 
                cloudInfo.ctime);
            
            CfOpenFileWithOplock(placeholderPath, 
                CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS | CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE,
                out SafeHCFFILE handle);

            long updateUsn = 0;
            HRESULT hres = CfUpdatePlaceholder(FileHandle: handle.DangerousGetHandle(),
                                FsMetadata: Placeholders.CreateFSMetadata(placeholderData, isDirectory),
                                FileIdentity: 0,
                                FileIdentityLength: 0,
                                DehydrateRangeCount: 0,
                                UpdateFlags: CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC,
                                UpdateUsn: ref updateUsn
                                );
            CfCloseHandle(handle);
        }

    }
}
