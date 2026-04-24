using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.Interfaces;
using OnedataDrive.Utils;
using System.Collections.Concurrent;

namespace OnedataDrive
{
    internal class BufferedEventMerger<T> : IDisposable where T : IEvent<T>
    {
        private ConcurrentQueue<T> input;
        private BufferExpirable<T> bufferExpirable;
        private CancellationTokenSource tokenSource;
        private IAddable<T> output;
        private Task queueReaderTask;
        private Task bufferFlusherTask;
        internal LoggerFormater loggerFormater;

        public bool isRunning;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        /// <param name="eventLifespan">In seconds</param>
        public BufferedEventMerger(IAddable<T> output, uint eventLifespan, Logger logger)
        {
            this.loggerFormater = new (logger);
            this.tokenSource = new();
            this.input = new();
            this.bufferExpirable = new BufferExpirable<T>(eventLifespan, loggerFormater);
            this.output = output;
            queueReaderTask = Task.Run(() => QueueReader(tokenSource.Token));
            bufferFlusherTask = Task.Run(() => BufferFlusher(tokenSource.Token));

            isRunning = true;

            loggerFormater.LogFileOP(LogLevel.Info, "BUFFERED EVENT MERGER", $"START - eventLifespan: {eventLifespan}");
        }

        public void AddEvent(T newEvent)
        {
            if (!isRunning)
            {
                throw new InvalidOperationException("BufferedEventMerger is not running.");
            }
            loggerFormater.LogFileOP(LogLevel.Info, "ADD EVENT", "Event detected", opID: newEvent.eventId, moreInfo: newEvent.MoreInfo());
            input.Enqueue(newEvent);
        }

        private void QueueReader(CancellationToken token, int cyclePeriod = 1000)
        {
            while (!token.IsCancellationRequested)
            {
                if (input.TryPeek(out T? newEvent))
                {
                    try
                    {
                        bufferExpirable.Add(newEvent);
                        input.TryDequeue(out _);
                    }
                    catch (Exception ex) when (ex is TimeoutException || ex is EventExpiredException)
                    {
                        loggerFormater.LogFileOP(LogLevel.Warn, "BUFFERED EVENT MERGER", "Queue reader - " +
                            "FAILED to add event to BufferExpirable - trying again", ex, opID: newEvent.eventId);
                        token.WaitHandle.WaitOne(cyclePeriod);
                    }
                    catch (Exception ex)
                    {
                        loggerFormater.LogFileOP(LogLevel.Error, "BUFFERED EVENT MERGER", "Queue reader - FAILED " +
                            "to add event to BufferExpirable - NOT PROCESSING this event", ex, opID: newEvent.eventId);
                        input.TryDequeue(out _);
                    }
                }
                else
                {
                    token.WaitHandle.WaitOne(cyclePeriod);
                }
            }
        }

        private void BufferFlusher(CancellationToken token, int cyclePeriod = 1000)
        {
            while (!token.IsCancellationRequested)
            {
                EventExpirable<T>? eventExpirable = null;
                try
                {
                    eventExpirable = bufferExpirable.PopOldestExpired();
                    if (eventExpirable is not null)
                    {
                        output.AddEvent(eventExpirable.@event);
                    }
                    else
                    {
                        token.WaitHandle.WaitOne(cyclePeriod);
                    }
                }
                catch (Exception e)
                {
                    List<string> moreInfo = new List<string>() { "FAILED to create more info" };
                    if (eventExpirable is not null)
                    {
                        moreInfo = new() { $"Event key: {eventExpirable.@event.RelationKey()}"};

                    }
                    loggerFormater.LogFileOP(LogLevel.Error, "BUFFERED EVENT MERGER", "BufferFlusher - FAILED " +
                            "to add event to output - NOT PROCESSING this event", e, opID: eventExpirable?.@event.eventId ?? "", moreInfo: moreInfo);
                }
            }
        }

        public void Dispose()
        {
            tokenSource.Cancel();
            queueReaderTask.Wait();
            bufferFlusherTask.Wait();
            isRunning = false;
        }

        internal class EventExpirable<S> where S : IEvent<S>
        {
            public S @event { get; private set; }
            public DateTime expirationUtc { get; private set; }
            private readonly object _lock = new();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="fileEvent"></param>
            /// <param name="lifespan">Lifespan length in seconds</param>
            public EventExpirable(S fileEvent, uint lifespan)
            {
                this.@event = fileEvent;
                this.expirationUtc = DateTime.UtcNow + new TimeSpan(0, 0, (int)lifespan);
            }

            public string MergeEvent(S newEvent)
            {
                if (!this.IsExpired())
                {
                    if (Monitor.TryEnter(_lock, 0))
                    {
                        try
                        {
                            @event.Merge(newEvent);
                            return @event.eventId;
                        }
                        finally
                        {
                            Monitor.Exit(_lock);
                        }
                    }
                    else
                    {
                        throw new TimeoutException($"Event is locked, EventId: {@event.eventId}");
                    }
                }
                else
                {
                    throw new EventExpiredException($"Event has expired, EventId: {@event.eventId}");
                }
            }

            public bool IsLocked()
            {
                return Monitor.IsEntered(_lock);
            }

            public bool IsExpired()
            {
                return expirationUtc <= DateTime.UtcNow;
            }
        }

        internal class BufferExpirable<U> where U : IEvent<U>
        {
            private ConcurrentDictionary<string, EventExpirable<U>> buffer;
            private ConcurrentQueue<string> expirationQueue;
            internal LoggerFormater loggFormater;
            public uint eventLifespan { get; private set; }

            public BufferExpirable(uint eventLifespan, LoggerFormater loggerFormater)
            {
                buffer = new();
                expirationQueue = new();
                this.eventLifespan = eventLifespan;
                this.loggFormater = loggerFormater;
            }

            public void Add(U newEvent)
            {
                if (buffer.TryGetValue(newEvent.RelationKey(), out EventExpirable<U>? fileEventExpirable))
                {
                    string mergedToEventId = fileEventExpirable.MergeEvent(newEvent);
                    List<string> moreInfo = new() { $"MergedToEventId: {mergedToEventId}" };
                    loggFormater.LogFileOP(LogLevel.Info, "BUFFERED EVENT MERGER", "Event merged", opID: newEvent.eventId,
                        moreInfo: moreInfo);
                }
                else
                {
                    string key = newEvent.RelationKey();
                    buffer[key] = new EventExpirable<U>(newEvent, eventLifespan);
                    expirationQueue.Enqueue(key);
                    loggFormater.LogFileOP(LogLevel.Debug, "BUFFERED EVENT MERGER", "Event added", opID: newEvent.eventId);
                }
            }

            public EventExpirable<U>? PopOldestExpired()
            {
                EventExpirable<U>? eventExpirable = null;

                if (expirationQueue.TryPeek(out string? fileId))
                {
                    if (buffer.TryGetValue(fileId, out eventExpirable))
                    {
                        if (eventExpirable.IsExpired() && !eventExpirable.IsLocked())
                        {
                            expirationQueue.TryDequeue(out _);
                            buffer.TryRemove(fileId, out eventExpirable);
                        }
                        else
                        {
                            eventExpirable = null;
                        }
                    }
                    else
                    {
                        expirationQueue.TryDequeue(out _);
                    }
                }
                return eventExpirable;
            }
        }
    }
}
