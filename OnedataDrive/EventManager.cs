using Microsoft.VisualBasic.FileIO;
using NLog;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Diagnostics;
using System.Security.Cryptography;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive
{
    internal class Event
    {
        internal FileEvent fileEvent;
        internal DateTime penalizedUntil { get; private set; }


        internal string? localFileName;

        public Event(FileEvent fileEvent)
        {
            this.fileEvent = fileEvent;
            this.penalizedUntil = DateTime.MinValue;

            this.localFileName = null;
        }

        public void Penalize(int penalty)
        {
            this.penalizedUntil = DateTime.UtcNow + TimeSpan.FromSeconds(penalty);
        }

        public bool IsPenalized()
        {
            if (penalizedUntil >= DateTime.UtcNow)
            {
                return true;
            }
            return false;
        }
    }

    internal class EventManager
    {
        public ThreadSafeList<Event> events;
        private CancellationTokenSource processingTokenSource;
        private AutoRefresh autoRefresh;
        private Task processingTask;

        public EventManager(AutoRefresh autoRefresh)
        {
            this.processingTokenSource = new();
            this.autoRefresh = autoRefresh;
            this.events = new ThreadSafeList<Event>();
            this.processingTask = Task.Run(() => ProcessEvents(processingTokenSource.Token, autoRefresh.spaceFolder.name));
            AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "CREATED",
                filePath: autoRefresh.spaceFolder.name);
        }

        public bool StopProcessing()
        {
            AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "Stop processing",
                filePath: autoRefresh.spaceFolder.name);
            processingTokenSource.Cancel();
            try
            {
                processingTask.Wait();
                AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "STOP OK",
                    filePath: autoRefresh.spaceFolder.name);
                return true;
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                    {
                        AutoRefresh.logFormatter.LogFileOP(LogLevel.Warn, "EVENT MANAGER", "stopped/canceled OK",
                            e, filePath: autoRefresh.spaceFolder.name);
                    }
                    else
                    {
                        AutoRefresh.logFormatter.LogFileOP(LogLevel.Error, "EVENT MANAGER", "STOP FAIL",
                            e, filePath: autoRefresh.spaceFolder.name);
                        return false;
                    }
                }
                return true;
            }
        }

        public void AddEvent(FileEvent fileEvent)
        {
            Event newEvent = new Event(fileEvent);
            if (fileEvent.eventType == FileEvent.EVENT_DELETED)
            {
                events.RemoveAll(e => e.fileEvent.fileId == fileEvent.fileId);
            }
            events.Add(newEvent);
        }

        /// <summary>
        /// Retrieves the file name corresponding to the specified file ID within the given directory.
        /// </summary>
        /// <remarks>This method searches the specified directory for a file whose placeholder ID matches
        /// the provided <paramref name="fileId"/>. If a match is found, the file name is returned. If no match is
        /// found, the method returns <see langword="null"/>.</remarks>
        /// <param name="fileId">The unique identifier of the file to locate.</param>
        /// <param name="parentDirPath">The path of the directory to search for the file.</param>
        /// <returns>The name of the file if a file with the specified ID is found; otherwise, <see langword="null"/>.</returns>
        private string? GetFileNameFromId(string fileId, string parentDirPath, out bool directory)
        {
            foreach (string filePath in Directory.EnumerateFileSystemEntries(parentDirPath))
            {
                if (PathUtils.GetPlaceholderId(filePath) == fileId)
                {
                    if (Directory.Exists(filePath))
                    {
                        directory = true;
                    }
                    else
                    {
                        directory = false;
                    }
                    return PathUtils.GetLastInPath(filePath);
                }
            }
            directory = false;
            return null;
        }

        private void ProcessEvents(CancellationToken cancellationToken, string spaceName)
        {
            const int SLEEP_INTERVAL = 2000;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (events.Count > 0)
                {
                    // find first non-penalized event
                    int index;
                    List<string> penalizedIds = new();
                    for (index = 0; index < events.Count; index++)
                    {
                        Event investigatedEvent = events[index];
                        if (investigatedEvent.IsPenalized())
                        {
                            penalizedIds.Add(investigatedEvent.fileEvent.fileId);
                            continue;
                        }
                        else if (penalizedIds.Contains(investigatedEvent.fileEvent.fileId))
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (index >= events.Count)
                    {
                        cancellationToken.WaitHandle.WaitOne(SLEEP_INTERVAL);
                        continue;
                    }

                    // process event
                    string opID = IdGenerator.GenerateId8();

                    Event processedEvent = events[index];

                    bool eventCompleted = ProcessEventWorker(processedEvent, opID);

                    if (!eventCompleted)
                    {
                        AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER",
                            "Event not processed - re-adding to the queue with penalty",
                            filePath: autoRefresh.spaceFolder.name, opID: opID);
                        processedEvent.Penalize(5);
                    }
                    else
                    {
                        events.RemoveAt(index);
                        AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "Event processed",
                            filePath: autoRefresh.spaceFolder.name, opID: opID);
                    }
                }
                else
                {
                    cancellationToken.WaitHandle.WaitOne(SLEEP_INTERVAL);
                }
            }
            AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "Process event stopped",
                        filePath: autoRefresh.spaceFolder.name);
        }

        private bool ProcessEventWorker(Event processedEvent, string opID)
        {
            bool eventCompleted = false;
            List<string> moreInfo = EventMoreInfo(processedEvent.fileEvent);
            try
            {
                string parentFolder = GetParentFolder(processedEvent.fileEvent);
                moreInfo.Add($"ParentFolder: {parentFolder}");
                AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "Processing event",
                    moreInfo: moreInfo, filePath: autoRefresh.spaceFolder.name, opID: opID);

                processedEvent.localFileName = GetFileNameFromId(processedEvent.fileEvent.fileId, parentFolder, out bool directory) ?? string.Empty;
                string filePath = Path.Combine(parentFolder, processedEvent.localFileName ?? string.Empty);

                switch (processedEvent.fileEvent.eventType)
                {
                    case FileEvent.EVENT_CHANGED:
                        // create
                        if (filePath == string.Empty)
                        {
                            List<ProviderInfo> providerInfos = autoRefresh.spaceFolder.providerInfos;
                            FileAttribute attribute = RestClient.GetFileAttribute(processedEvent.fileEvent.fileId, providerInfos).Result;
                            using (PlaceholderCreateInfo createInfo = new())
                            {
                                PlaceholderData placeholderData = new(attribute);
                                createInfo.Add(Placeholders.CreateInfo(placeholderData));
                                CF_PLACEHOLDER_CREATE_INFO[] infoArr = createInfo.GetArray();
                                HRESULT hres = CfCreatePlaceholders(parentFolder, infoArr, (uint)infoArr.Length,
                                    CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out uint entriesProcessed);
                                if (hres == HRESULT.S_OK || entriesProcessed == infoArr.Length)
                                {
                                    eventCompleted = true;
                                    Debug.Print($"File Created: {processedEvent.fileEvent.fileId}");
                                }
                            }
                        }
                        // update
                        else
                        {
                            UpdatePlaceholderMetadata(processedEvent, filePath, directory);
                            eventCompleted = true;
                        }    
                        break;
                    case FileEvent.EVENT_DELETED:
                        if (filePath == string.Empty)
                        {
                            eventCompleted = true;
                        }
                        else
                        {
                            if (directory)
                            {
                                Directory.Delete(filePath, true);
                            }
                            else
                            {
                                File.Delete(filePath);
                            }
                            eventCompleted = true;
                        }
                        break;
                    default:
                        AutoRefresh.logFormatter.LogFileOP(LogLevel.Error, "EVENT MANAGER",
                            $"Unknown event type: {processedEvent.fileEvent.eventType}",
                            moreInfo: moreInfo, filePath: autoRefresh.spaceFolder.name, opID: opID);
                        break;
                }
            }
            catch (Exception e)
            {
                AutoRefresh.logFormatter.LogFileOP(LogLevel.Error, "EVENT MANAGER", "Process event error", e,
                    filePath: autoRefresh.spaceFolder.name, opID: opID);
            }
            return eventCompleted;
        }

        private void UpdatePlaceholderMetadata(Event processedEvent, string placeholderPath, bool directory)
        {
            CF_FS_METADATA metadata = new();
            if (processedEvent.fileEvent.data.size is not null) metadata.FileSize = (long)processedEvent.fileEvent.data.size;

            SafeHCFFILE? handle = null;
            CF_OPEN_FILE_FLAGS flags = CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE;
            if (!directory)
            {
                flags = CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE;
            }
            try
            {
                HRESULT openHres = CfOpenFileWithOplock(placeholderPath,
                    flags,
                    out handle);
                if (openHres != HRESULT.S_OK)
                {
                    throw new Exception($"CfOpenFileWithOplock HRES number: {((int)openHres)}" +
                        $"\n HRES text: {openHres}");
                }
                long updateUsn = 0;
                HRESULT updateHres = CfUpdatePlaceholder(FileHandle: handle.DangerousGetHandle(),
                                    FsMetadata: metadata,
                                    FileIdentity: 0,
                                    FileIdentityLength: 0,
                                    DehydrateRangeCount: 0,
                                    UpdateFlags: CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC,
                                    UpdateUsn: ref updateUsn
                                    );
                if (updateHres != HRESULT.S_OK)
                {
                    throw new Exception($"CfUpdatePlaceholder HRES number: {((int)updateHres)}" +
                        $"\n HRES text: {updateHres}");
                }

                // rename if needed
                if (processedEvent.fileEvent.data.name is not null)
                {
                    if (PathUtils.GetLastInPath(placeholderPath) != processedEvent.fileEvent.data.name)
                    {
                        if (directory)
                        {
                            FileSystem.RenameDirectory(placeholderPath, processedEvent.fileEvent.data.name);
                        }
                        else
                        {
                            FileSystem.RenameFile(placeholderPath, processedEvent.fileEvent.data.name);
                        }
                        processedEvent.localFileName = processedEvent.fileEvent.data.name;
                    }
                    HRESULT inSyncHres = CfSetInSyncState(handle.DangerousGetHandle(),
                        CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
                    if (inSyncHres != HRESULT.S_OK)
                    {
                        throw new Exception($"CfSetInSync HRES number: {((int)inSyncHres)}" +
                            $"\n HRES text: {inSyncHres}");
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (handle != null && !handle.IsInvalid)
                {
                    handle.Dispose();
                }
            }
        }

        private string GetParentFolder(FileEvent fileEvent)
        {
            return autoRefresh.monitored
                .First(m => m.id == fileEvent.parentFileId).path;
        }

        private List<string> EventMoreInfo(FileEvent fileEvent)
        {
            return new List<string> {
                $"EventId: {fileEvent.eventId}",
                $"EventType: {fileEvent.eventType}",
                $"FileId: {fileEvent.fileId}",
                $"ParentId: {fileEvent.parentFileId}" };
        }
    }

    internal enum EventType
    {
        Unknown = 0,
        Updated = 1,
        Renamed = 2,
        Created = 3,
        Deleted = 4
    }
}
