using NLog;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;

namespace OnedataDrive.Interfaces
{
    internal class Event<T>
    {
        internal T @event;
        internal DateTime penalizedUntil { get; private set; }
        internal int penalizedCount { get; private set; }

        internal string? localFileName;

        public Event(T @event)
        {
            this.@event = @event;
            this.penalizedUntil = DateTime.MinValue;
            this.penalizedCount = 0;

            this.localFileName = null;
        }

        public void Penalize(int penalty)
        {
            this.penalizedUntil = DateTime.UtcNow + TimeSpan.FromSeconds(penalty);
            this.penalizedCount += 1;
        }

        public bool IsPenalized()
        {
            if (penalizedUntil >= DateTime.UtcNow)
            {
                return true;
            }
            return false;
        }

        public string EventFileId()
        {
            FileEvent? fileEvent = @event as FileEvent;
            if (fileEvent is not null)
            {
                return fileEvent.fileId;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    abstract class EventProcessor<T>
    {
        protected LoggerFormater logFormatter;
        protected string spaceName;

        protected ThreadSafeList<Event<T>> events;
        private CancellationTokenSource processingTokenSource;
        protected Task processingTask { get; private set; }

        public EventProcessor(Logger logger, string spaceName)
        {
            this.spaceName = spaceName;
            this.logFormatter = new(logger); 
            this.processingTokenSource = new();
            this.events = new ThreadSafeList<Event<T>>();
            this.processingTask = Task.Run(() => ProcessEvents(processingTokenSource.Token));
            logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "CREATED",
                filePath: spaceName);
        }

        public bool StopProcessing()
        {
            logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Stop processing",
                filePath: spaceName);
            processingTokenSource.Cancel();
            try
            {
                processingTask.Wait();
                logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "STOP OK",
                    filePath: spaceName);
                return true;
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                    {
                        logFormatter.LogFileOP(LogLevel.Warn, "EVENT PROCESSOR", "stopped/canceled OK",
                            e, filePath: spaceName);
                    }
                    else
                    {
                        logFormatter.LogFileOP(LogLevel.Error, "EVENT PROCESSOR", "STOP FAIL",
                            e, filePath: spaceName);
                        return false;
                    }
                }
                return true;
            }
        }

        public void AddEvent(T fileEvent)
        {
            Event<T> newEvent = new Event<T>(fileEvent);
            events.Add(newEvent);
        }

        protected virtual int PenaltyTimeCreator(int penalizedCount)
        {
            const int DEFAULT_PENALTY = 5;
            int penaltyMultiplier = penalizedCount / 3 + 1;
            return penaltyMultiplier * DEFAULT_PENALTY;
        }


        protected void ProcessEvents(CancellationToken cancellationToken, int sleepInterval = 2000)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (events.Count > 0)
                {
                    // find first non-penalized event
                    int index;
                    List<string> penalizedIds = new();
                    for (index = 0; index < events.Count; index++)
                    {
                        Event<T> investigatedEvent = events[index];
                        if (investigatedEvent.IsPenalized())
                        {
                            string eventId = investigatedEvent.EventFileId();
                            if (!string.IsNullOrEmpty(eventId))
                            {
                                penalizedIds.Add(eventId);
                            }
                            continue;
                        }
                        else if (penalizedIds.Contains(investigatedEvent.EventFileId()))
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
                        cancellationToken.WaitHandle.WaitOne(sleepInterval);
                        continue;
                    }

                    // process event
                    string opID = IdGenerator.GenerateId8();

                    Event<T> processedEvent = events[index];

                    bool eventCompleted = ProcessEventWorker(processedEvent, opID);

                    if (!eventCompleted)
                    {
                        int penaltyTime = PenaltyTimeCreator(processedEvent.penalizedCount);
                        processedEvent.Penalize(penaltyTime);
                        logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR",
                            $"Event not processed - re-adding to the queue with penalty of {penaltyTime}s",
                            filePath: spaceName, opID: opID);
                    }
                    else
                    {
                        events.RemoveAt(index);
                        logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Event processed",
                            filePath: spaceName, opID: opID);
                    }
                }
                else
                {
                    cancellationToken.WaitHandle.WaitOne(sleepInterval);
                }
            }
            logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Process event stopped",
                        filePath: spaceName);
        }

        protected abstract bool ProcessEventWorker(Event<T> processedEvent, string opID);
       
    }
}

