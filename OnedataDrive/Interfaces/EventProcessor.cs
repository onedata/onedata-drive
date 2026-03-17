using NLog;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;

namespace OnedataDrive.Interfaces
{
    internal class EventPenalizable<T> where T : IEvent<T>
    {
        internal T @event;
        internal DateTime penalizedUntil { get; private set; }
        internal int penalizedCount { get; private set; }

        internal string? localFileName;

        public EventPenalizable(T @event)
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
    }

    abstract class EventProcessor<T> where T : IEvent<T>
    {
        protected LoggerFormater logFormatter;
        protected string spaceNameWPrefix;

        protected ThreadSafeList<EventPenalizable<T>> events;
        private CancellationTokenSource processingTokenSource;
        protected Task processingTask { get; private set; }
        protected int sleepInterval { get; private set; }

        public EventProcessor(Logger logger, string spaceName = "", int sleepInterval = 2000)
        {
            this.spaceNameWPrefix = "Spc: " + spaceName;
            this.logFormatter = new(logger); 
            this.sleepInterval = sleepInterval;
            this.processingTokenSource = new();
            this.events = new ThreadSafeList<EventPenalizable<T>>();
            this.processingTask = Task.Run(() => ProcessEvents(processingTokenSource.Token));
            logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "CREATED",
                filePath: spaceName);
        }

        public bool StopProcessing()
        {
            logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Stop processing - request",
                filePath: spaceNameWPrefix);
            processingTokenSource.Cancel();
            try
            {
                processingTask.Wait();
                logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "STOP OK",
                    filePath: spaceNameWPrefix);
                return true;
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                    {
                        logFormatter.LogFileOP(LogLevel.Warn, "EVENT PROCESSOR", "stopped/canceled OK",
                            e, filePath: spaceNameWPrefix);
                    }
                    else
                    {
                        logFormatter.LogFileOP(LogLevel.Error, "EVENT PROCESSOR", "STOP FAIL",
                            e, filePath: spaceNameWPrefix);
                        return false;
                    }
                }
                return true;
            }
        }

        public void AddEvent(T fileEvent)
        {
            EventPenalizable<T> newEvent = new EventPenalizable<T>(fileEvent);
            events.Add(newEvent);
        }

        protected virtual int PenaltyTimeCreator(int penalizedCount)
        {
            const int DEFAULT_PENALTY = 5;
            int penaltyMultiplier = penalizedCount / 3 + 1;
            return penaltyMultiplier * DEFAULT_PENALTY;
        }


        protected void ProcessEvents(CancellationToken cancellationToken)
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
                        EventPenalizable<T> investigatedEvent = events[index];
                        if (investigatedEvent.IsPenalized())
                        {
                            string eventId = investigatedEvent.@event.RelationKey();
                            if (!string.IsNullOrEmpty(eventId))
                            {
                                penalizedIds.Add(eventId);
                            }
                            continue;
                        }
                        else if (penalizedIds.Contains(investigatedEvent.@event.RelationKey()))
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
                    EventPenalizable<T> processedEvent = events[index];
                    

                    bool eventCompleted = ProcessEventWorker(processedEvent);

                    if (!eventCompleted)
                    {
                        int penaltyTime = PenaltyTimeCreator(processedEvent.penalizedCount);
                        if (penaltyTime < 0)
                        {
                            logFormatter.LogFileOP(LogLevel.Error, "EVENT PROCESSOR", 
                                "Event not processed - giving up", filePath: spaceNameWPrefix, opID: processedEvent.@event.eventId);
                            events.RemoveAt(index);
                            continue;
                        }
                        processedEvent.Penalize(penaltyTime);
                        logFormatter.LogFileOP(LogLevel.Warn, "EVENT PROCESSOR",
                            $"Event not processed - re-adding to the queue with penalty of {penaltyTime}s",
                            filePath: spaceNameWPrefix, opID: processedEvent.@event.eventId);
                    }
                    else
                    {
                        events.RemoveAt(index);
                        logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Event processed",
                            filePath: spaceNameWPrefix, opID: processedEvent.@event.eventId);
                    }
                }
                else
                {
                    cancellationToken.WaitHandle.WaitOne(sleepInterval);
                }
            }
            logFormatter.LogFileOP(LogLevel.Info, "EVENT PROCESSOR", "Process event stopped",
                        filePath: spaceNameWPrefix);
        }

        protected abstract bool ProcessEventWorker(EventPenalizable<T> processedEvent);
       
    }
}

