using Microsoft.VisualBasic.FileIO;
using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.Interfaces;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Diagnostics;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive
{
    internal class FileEventProcessor : EventProcessor<FileEvent>, IAddable<FileEvent>
    {
        private AutoRefresh autoRefresh;

        public FileEventProcessor(AutoRefresh autoRefresh) : base(autoRefresh.logger, autoRefresh.spaceFolder.name)
        {
            this.autoRefresh = autoRefresh;
        }

        public new void AddEvent(FileEvent fileEvent)
        {
            Event<FileEvent> newEvent = new Event<FileEvent>(fileEvent);
            if (fileEvent.eventType == FileEvent.EVENT_DELETED)
            {
                events.RemoveAll(e => e.@event.fileId == fileEvent.fileId);
            }
            events.Add(newEvent);
        }

        protected override int PenaltyTimeCreator(int penalizedCount)
        {
            const int DEFAULT_PENALTY = 5;
            int penaltyMultiplier = penalizedCount / 3 + 1;
            return penaltyMultiplier * DEFAULT_PENALTY;
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

        protected override bool ProcessEventWorker(Event<FileEvent> processedEvent, string opID)
        {
            bool eventCompleted = false;
            List<string> moreInfo = EventMoreInfo(processedEvent.@event);
            try
            {
                string parentFolder = GetParentFolder(processedEvent.@event);
                moreInfo.Add($"ParentFolder: {parentFolder}");

                processedEvent.localFileName = GetFileNameFromId(processedEvent.@event.fileId, parentFolder, out bool directory) ?? string.Empty;
                moreInfo.Add($"LocalFileName: {processedEvent.localFileName}");

                string filePath = Path.Combine(parentFolder, processedEvent.localFileName ?? string.Empty);

                switch (processedEvent.@event.eventType)
                {
                    case FileEvent.EVENT_CHANGED:
                        // create
                        if (string.IsNullOrWhiteSpace(processedEvent.localFileName))
                        {
                            logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Processing event - Create new",
                                moreInfo: moreInfo, opID: opID, filePath: spaceName);
                            List<ProviderInfo> providerInfos = autoRefresh.spaceFolder.providerInfos;
                            FileAttribute attribute = RestClient.GetFileAttribute(processedEvent.@event.fileId, providerInfos).Result;
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
                                    Debug.Print($"File Created: {processedEvent.@event.fileId}");
                                }
                            }
                        }
                        // update
                        else
                        {
                            logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Processing event - Update",
                                moreInfo: moreInfo, opID: opID, filePath: spaceName);
                            UpdatePlaceholderMetadata(processedEvent, filePath, directory, opID);
                            eventCompleted = true;
                        }    
                        break;
                    case FileEvent.EVENT_DELETED:
                        // delete
                        logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Processing event - Delete",
                                moreInfo: moreInfo,opID: opID, filePath: spaceName);
                        if (string.IsNullOrWhiteSpace(processedEvent.localFileName))
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
                        logFormatter.LogFileOP(LogLevel.Error, "EVENT PROCESSOR",
                            $"Unknown event type - Discarding event: {processedEvent.@event.eventType}",
                            moreInfo: moreInfo, filePath: spaceName, opID: opID);
                        eventCompleted = true;
                        break;
                }
            }
            catch (NoSuchCloudFile e)
            {
                eventCompleted = true;
                logFormatter.LogFileOP(LogLevel.Warn, "EVENT PROCESSOR", "File does not exist on cloud anymore",
                    e, moreInfo: moreInfo, filePath: spaceName, opID: opID);
            }
            catch (ThreadSafeMonitored.DirectoryNotMonitoredException e)
            {
                eventCompleted = true;
                logFormatter.LogFileOP(LogLevel.Warn, "EVENT PROCESSOR", "Parent folder not monitored - Unknown parent id - discarding event",
                    e, moreInfo: moreInfo, filePath: spaceName, opID: opID);
            }
            catch (Exception e)
            {
                logFormatter.LogFileOP(LogLevel.Error, "EVENT PROCESSOR", "Process event error", e,
                    filePath: spaceName, opID: opID);
            }
            return eventCompleted;
        }

        private void UpdatePlaceholderMetadata(Event<FileEvent> processedEvent, string placeholderPath, bool directory, string opID)
        {
            CF_FS_METADATA metadata = new();
            if (processedEvent.@event.data.size is not null) metadata.FileSize = (long)processedEvent.@event.data.size;

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
                if (processedEvent.@event.data.name is not null)
                {
                    string oldName = PathUtils.GetLastInPath(placeholderPath);
                    string newName = processedEvent.@event.data.name;
                    if (oldName != newName)
                    {
                        if (directory)
                        {
                            FileSystem.RenameDirectory(placeholderPath, newName);
                        }
                        else
                        {
                            FileSystem.RenameFile(placeholderPath, newName);
                        }
                        processedEvent.localFileName = newName;
                        HRESULT inSyncHres = CfSetInSyncState(handle.DangerousGetHandle(),
                        CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
                        if (inSyncHres != HRESULT.S_OK)
                        {
                            throw new Exception($"CfSetInSync HRES number: {((int)inSyncHres)}" +
                                $"\n HRES text: {inSyncHres}");
                        }
                        List<string> moreInfo = new List<string>() { $"{oldName} -> {newName}" };
                        logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Placeholder renamed", moreInfo: moreInfo,
                            opID: opID, filePath: spaceName);
                    }
                }
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
            ThreadSafeMonitored monitored = autoRefresh.monitored;
            try
            {
                return monitored
                .First(m => m.id == fileEvent.parentFileId).path;
            }
            catch (InvalidOperationException e)
            {
                if (!monitored.Any(m => m.id == fileEvent.parentFileId))
                {
                    throw new ThreadSafeMonitored.DirectoryNotMonitoredException(
                        $"Parent folder with id {fileEvent.parentFileId} not found in monitored folders.", e);
                }
                else
                {
                    throw;
                }
            }
            
        }

        private List<string> EventMoreInfo(FileEvent fileEvent)
        {
            return new List<string> {
                $"Merged: {fileEvent.isMerged}",
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
