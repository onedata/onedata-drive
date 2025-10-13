using OnedataDrive.JSON_Object;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace OnedataDrive
{
    internal class BufferedEventMerger
    {
        internal class FileEventExpirable
        {
            internal class FileEventExpiredException : Exception
            {
                public FileEventExpiredException(string message) : base(message) { }
                public FileEventExpiredException(string message, Exception inner) : base(message, inner) { }
            }

            public FileEvent fileEvent { get; private set; }
            public DateTime expirationUtc { get; private set; }
            private readonly object _lock = new();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="fileEvent"></param>
            /// <param name="lifespanLength">Lifespan length in seconds</param>
            public FileEventExpirable(FileEvent fileEvent, uint lifespanLength)
            {
                this.fileEvent = fileEvent;
                this.expirationUtc = DateTime.UtcNow + new TimeSpan(0, 0, (int)lifespanLength);
            }

            public void MergeEvent(FileEvent newEvent)
            {
                if (this.IsExpired())
                {
                    if (Monitor.TryEnter(_lock, 0))
                    {
                        try
                        {
                            fileEvent.Merge(newEvent);
                            return;
                        }
                        finally
                        {
                            Monitor.Exit(_lock);
                        }
                    }
                    else
                    {
                        throw new TimeoutException("Event is locked");
                    }
                }
                else
                {
                    throw new FileEventExpiredException("Event has expired");
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

        internal class BufferExpirable
        {
            private ConcurrentDictionary<string, FileEventExpirable> buffer;
            private ConcurrentQueue<string> expirationQueue;
            public uint eventLifespan { get; private set; }

            public BufferExpirable(uint eventLifespan)
            {
                buffer = new();
                expirationQueue = new();
                this.eventLifespan = eventLifespan;
            }

            public void Add(FileEvent fileEvent)
            {
                if (buffer.TryGetValue(fileEvent.fileId, out FileEventExpirable? fileEventExpirable))
                {
                    fileEventExpirable.MergeEvent(fileEvent);
                }
                else
                {
                    string fileId = fileEvent.fileId;
                    buffer[fileId] = new FileEventExpirable(fileEvent, eventLifespan);
                    expirationQueue.Enqueue(fileId);
                }
            }

            public FileEventExpirable? PopOldestExpired()
            {
                FileEventExpirable? fileEventExpirable = null;

                if (expirationQueue.TryPeek(out string? fileId))
                {   
                    if (buffer.TryGetValue(fileId, out fileEventExpirable))
                    {
                        if (fileEventExpirable.IsExpired() && !fileEventExpirable.IsLocked())
                        {
                            expirationQueue.TryDequeue(out _);
                            buffer.TryRemove(fileId, out fileEventExpirable);
                        }
                        else
                        {
                            fileEventExpirable = null;
                        }
                    }
                    else
                    {
                        expirationQueue.TryDequeue(out _);
                    }
                }
                return fileEventExpirable;
            }
        }

        private ConcurrentQueue<FileEvent> input;
        private BufferExpirable bufferExpirable;
        private readonly uint eventLifespan;
        private CancellationTokenSource tokenSource;
        private EventManager output;
        private Task queueReaderTask;
        private Task bufferFlusherTask;

        public bool isRunning;

        public BufferedEventMerger(EventManager output, uint eventLifespan)
        {
            this.tokenSource = new();
            this.input = new();
            this.bufferExpirable = new BufferExpirable(eventLifespan);
            this.output = output;
            this.eventLifespan = eventLifespan;
            queueReaderTask = Task.Run(() => QueueReader(tokenSource.Token));
            bufferFlusherTask = Task.Run(() => BufferFlusher(tokenSource.Token));

            isRunning = true;
            
        }

        public void AddEvent(FileEvent fileEvent)
        {
            if (!isRunning)
            {
                throw new InvalidOperationException("BufferedEventMerger is not running.");
            }
            input.Enqueue(fileEvent);
        }

        private void QueueReader(CancellationToken token, int cyclePeriod = 1000)
        {
            while (!token.IsCancellationRequested)
            {
                if (input.TryPeek(out FileEvent? fileEvent))
                {
                    try
                    {
                        bufferExpirable.Add(fileEvent);
                        input.TryDequeue(out _);
                    }
                    catch (Exception ex)
                    {
                        // Log the exception
                        Debug.Print($"Error processing event: {ex.Message}");
                        token.WaitHandle.WaitOne(cyclePeriod);
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
                FileEventExpirable? fileEventExpirable = bufferExpirable.PopOldestExpired();
                if (fileEventExpirable is not null)
                {
                    output.AddEvent(fileEventExpirable.fileEvent);
                }
                else
                {
                    token.WaitHandle.WaitOne(cyclePeriod);
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
    }
}
