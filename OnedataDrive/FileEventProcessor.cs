using Microsoft.VisualBasic.FileIO;
using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.Interfaces;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive
{
    internal class FileEventProcessor : EventProcessor<FileEvent>, IAddable<FileEvent>
    {
        private AutoRefresh autoRefresh;

        public FileEventProcessor(AutoRefresh autoRefresh) : base(autoRefresh.logger, spaceName: autoRefresh.spaceFolder.name)
        {
            this.autoRefresh = autoRefresh;
        }

        public new void AddEvent(FileEvent fileEvent)
        {
            EventPenalizable<FileEvent> newEvent = new EventPenalizable<FileEvent>(fileEvent);
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

        private void TestIfCanBeCreated(FileAttribute attribute)
        {
            if (attribute.name == ".trash")
            {
                throw new InvalidFileEventException($"Prohibited file/direcotry name: {attribute.name}");
            }
        }

        protected override bool ProcessEventWorker(EventPenalizable<FileEvent> processedEvent)
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
                                moreInfo: moreInfo, opID: processedEvent.@event.eventId, filePath: spaceNameWPrefix);
                            List<ProviderInfo> providerInfos = autoRefresh.spaceFolder.providerInfos;
                            FileAttribute attribute = RestClient.GetFileAttribute(processedEvent.@event.fileId, providerInfos).Result;

                            logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Processing event - Create new",
                                moreInfo: [$"New placeholder name: {attribute.name}", $"Type: {attribute.type}"], opID: processedEvent.@event.eventId, filePath: spaceNameWPrefix);

                            TestIfCanBeCreated(attribute);

                            using (PlaceholderCreateInfoList createInfo = new())
                            {
                                PlaceholderData placeholderData = new(attribute);
                                createInfo.Add(PlaceholderData.CreateInfo(placeholderData));
                                CF_PLACEHOLDER_CREATE_INFO[] infoArr = createInfo.GetArray();
                                HRESULT hres = CfCreatePlaceholders(parentFolder, infoArr, (uint)infoArr.Length,
                                    CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out uint entriesProcessed);
                                if (hres == HRESULT.S_OK || entriesProcessed == infoArr.Length)
                                {
                                    eventCompleted = true;
                                }
                            }
                        }
                        // update
                        else
                        {
                            logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Processing event - Update",
                                moreInfo: moreInfo, opID: processedEvent.@event.eventId, filePath: spaceNameWPrefix);
                            UpdatePlaceholderMetadata(processedEvent, filePath, directory, processedEvent.@event.eventId);
                            eventCompleted = true;
                        }    
                        break;
                    case FileEvent.EVENT_DELETED:
                        // delete
                        logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Processing event - Delete",
                                moreInfo: moreInfo,opID: processedEvent.@event.eventId, filePath: spaceNameWPrefix);
                        if (string.IsNullOrWhiteSpace(processedEvent.localFileName))
                        {
                            eventCompleted = true;
                        }
                        else
                        {
                            if (directory)
                            {
                                DirectoryOD.Delete(filePath, processedEvent.@event.fileId);
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
                            moreInfo: moreInfo, filePath: spaceNameWPrefix, opID: processedEvent.@event.eventId);
                        eventCompleted = true;
                        break;
                }
            }
            catch (InvalidFileEventException e)
            {
                eventCompleted = true;
                logFormatter.LogFileOP(LogLevel.Warn, "EVENT PROCESSOR", "Invalid file event - discarding event",
                    e, moreInfo: moreInfo, filePath: spaceNameWPrefix, opID: processedEvent.@event.eventId);
            }
            catch (Exception e) when (e is NoSuchCloudFile || e is AggregateException && e.InnerException is NoSuchCloudFile)
            {
                eventCompleted = true;
                logFormatter.LogFileOP(LogLevel.Warn, "EVENT PROCESSOR", "File does not exist on cloud anymore - discarding event",
                    e, moreInfo: moreInfo, filePath: spaceNameWPrefix, opID: processedEvent.@event.eventId);
            }
            catch (ThreadSafeMonitored.DirectoryNotMonitoredException e)
            {
                eventCompleted = true;
                logFormatter.LogFileOP(LogLevel.Warn, "EVENT PROCESSOR", "Parent folder not monitored - Unknown parent id - discarding event",
                    e, moreInfo: moreInfo, filePath: spaceNameWPrefix, opID: processedEvent.@event.eventId);
            }
            catch (DirectoryNotFoundException e)
            {
                eventCompleted = true;
                logFormatter.LogFileOP(LogLevel.Warn, "EVENT PROCESSOR", "Directory not found - discarding event",
                    e, moreInfo: moreInfo, filePath: spaceNameWPrefix, opID: processedEvent.@event.eventId);
            }
            catch (ArgumentException e)
            {
                eventCompleted = true;
                logFormatter.LogFileOP(LogLevel.Warn, "EVENT PROCESSOR", "Invalid argument - discarding event",
                    e, moreInfo: moreInfo, filePath: spaceNameWPrefix, opID: processedEvent.@event.eventId);
            }
            catch (Exception e)
            {
                logFormatter.LogFileOP(LogLevel.Error, "EVENT PROCESSOR", "Process event error", e,
                    filePath: spaceNameWPrefix, opID: processedEvent.@event.eventId);
            }
            return eventCompleted;
        }

        private void UpdatePlaceholderMetadata(EventPenalizable<FileEvent> processedEvent, string placeholderPath, bool directory, string opID)
        {
            CF_FS_METADATA metadata = new();
            if (processedEvent.@event.data.size is not null) 
            { 
                metadata.FileSize = (long)processedEvent.@event.data.size;
            }
            if (processedEvent.@event.data.mtime is not null) 
            {
                DateTime mtime = DateTimeOffset.FromUnixTimeSeconds(processedEvent.@event.data.mtime ?? 0).UtcDateTime;
                metadata.BasicInfo.LastWriteTime = new FILETIME
                {
                    dwHighDateTime = (int)mtime.ToFileTime().HighPart(),
                    dwLowDateTime = (int)mtime.ToFileTime().LowPart()
                };
            }

            CF_OPEN_FILE_FLAGS flags = CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE;
            if (!directory)
            {
                flags = CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE;
            }

            using CfHandle handle = new(placeholderPath, flags);
            CF_PLACEHOLDER_BASIC_INFO standardInfo = CldApiUtils.GetBasicInfo(handle);

            CF_FILE_RANGE[] dehydrateRanges = [];
            if (!directory && processedEvent.@event.data.mtime != null)
            {
                FileInfo fileInfo = new(placeholderPath);
                DateTime lastWriteTime = fileInfo.LastWriteTimeUtc;

                DateTime cloudMTime = DateTimeOffset.FromUnixTimeSeconds((long)processedEvent.@event.data.mtime).UtcDateTime;
                if (lastWriteTime != cloudMTime)
                {
                    dehydrateRanges = [new CF_FILE_RANGE { StartingOffset = 0, Length = -1 }];
                }
            }

            long updateUsn = 0;
            
            HRESULT updateHres = CfUpdatePlaceholder(FileHandle: handle.GetDangerousHandle(),
                                FsMetadata: metadata,
                                FileIdentity: 0,
                                FileIdentityLength: 0,
                                DehydrateRangeArray: dehydrateRanges,
                                DehydrateRangeCount: (uint)dehydrateRanges.Length,
                                UpdateFlags: CF_UPDATE_FLAGS.CF_UPDATE_FLAG_NONE,
                                UpdateUsn: ref updateUsn
                                );
            if (updateHres != HRESULT.S_OK)
            {
                throw new Exception($"CfUpdatePlaceholder HRES number: 0x{((uint)updateHres):X}" +
                    $"\n HRES text: {updateHres}");
            }

            if (dehydrateRanges.Length > 0)
            {
                logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Placeholder data invalidated", 
                    opID: opID, filePath: spaceNameWPrefix);
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
                        autoRefresh.RenameMonitored(processedEvent.@event.fileId, newName);
                    }
                    else
                    {
                        FileSystem.RenameFile(placeholderPath, newName);
                    }
                    processedEvent.localFileName = newName;

                    string newPath = Path.Combine(PathUtils.GetParentPath(placeholderPath), newName);
                    using CfHandle handleNewPath = new(newPath, flags);

                    HRESULT inSyncHres = CfSetInSyncState(handleNewPath.GetDangerousHandle(),
                    standardInfo.InSyncState, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
                    if (inSyncHres != HRESULT.S_OK)
                    {
                        throw new Exception($"CfSetInSync HRES number: {((int)inSyncHres)}" +
                            $"\n HRES text: {inSyncHres}");
                    }
                    List<string> moreInfo = new List<string>() { $"{oldName} -> {newName}" };
                    logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Placeholder renamed", moreInfo: moreInfo,
                        opID: opID, filePath: spaceNameWPrefix);
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
                $"EventId: {fileEvent.SSEventId}",
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
