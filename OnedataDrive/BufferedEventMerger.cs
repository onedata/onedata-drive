using OnedataDrive.JSON_Object;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                            // update logic here
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

            private void Merge(FileEvent newer)
            {
                FileEvent updated;
                
            }
        }

        internal class BufferExpirable
        {
            internal ConcurrentDictionary<string, FileEventExpirable> buffer;
            internal ConcurrentQueue<string> expirationQueue;
            internal uint eventLifespan;

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

            public FileEvent? PopOldest()
            {
                FileEventExpirable? fileEventExpirable = null;
                if (expirationQueue.TryDequeue(out string? fileId))
                {
                    buffer.TryRemove(fileId, out fileEventExpirable);   
                }
                return fileEventExpirable?.fileEvent;
            }
        }

        private ConcurrentQueue<FileEvent> input;
        private BufferExpirable bufferExpirable;
        private uint eventLifespan;
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
                if (bufferExpirable.expirationQueue.TryPeek(out string? id))
                {
                    FileEventExpirable fileEvent = bufferExpirable.buffer[id];
                    if (fileEvent.IsExpired() && !fileEvent.IsLocked())
                    {
                        output.AddEvent(fileEvent.fileEvent);
                        bufferExpirable.PopOldest();
                    }
                    else 
                    {
                        token.WaitHandle.WaitOne(cyclePeriod);
                    }
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
            isRunning = false;
            throw new NotImplementedException();
        }
    }
}
