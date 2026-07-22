using NLog;
using OnedataDrive.Utils;
using System.Diagnostics;

namespace OnedataDrive
{
    public class RunningTaksList
    {
        private CancellationTokenSource? masterCTS;
        private LoggerFormater loggerFormater;

        public ThreadSafeList<RunningTask> list { get; private set; }
        public Task? listCleaner { get; private set; }
        public int cleanerPeriodSeconds { get; private set; }
        public bool isInitialized { get; private set; } = false;

        public RunningTaksList(uint cleanerPeriodSeconds)
        {
            this.loggerFormater = new LoggerFormater(LogManager.GetCurrentClassLogger());
            this.cleanerPeriodSeconds = (int)cleanerPeriodSeconds;
            this.list = new ThreadSafeList<RunningTask>();
        }

        public void Initialize()
        {
            if (!isInitialized)
            {
                this.masterCTS = new CancellationTokenSource();
                listCleaner = Task.Run(() => CleanerTask(masterCTS.Token, cleanerPeriodSeconds));
                isInitialized = true;
            }
        }

        /// <summary>
        /// Stop list cleaner task and cancel all Running tasks in the list. 
        /// After calling this method, the RunningTaksList instance should not be used anymore.
        /// If you want to use RunningTaksList again call Initialize()
        /// </summary>
        public void Dispose()
        {
            if (isInitialized)
            {
                masterCTS?.Cancel();
                this.isInitialized = false;
            }
        }

        public RunningTask AddTask(Func<CancellationToken, Task> funcToStart, TaskType type, string opID)
        {
            return AddTask(funcToStart, type, null, opID);
        }

        public RunningTask AddTask(Func<CancellationToken, Task> funcToStart, TaskType type, Callback? callback, string opID)
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("RunningTaksList is not initialized. Call Initialize() before adding tasks.");
            }

            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(masterCTS!.Token);
            Task newTask = Task.Run(() => funcToStart(cts.Token));
            RunningTask runningTask = new(type, newTask, cts, opID, callback);
            this.list.Add(runningTask);
            return runningTask;
        }

        private void CleanerTask(CancellationToken token, int periodSeconds)
        {
            while (!token.IsCancellationRequested)
            {
                list.RemoveAll(rt => rt.task.IsCompleted);
                token.WaitHandle.WaitOne(periodSeconds * 1000);
            }
            loggerFormater.LogFileOP(LogLevel.Debug, "LIST CLEANER", "STOP");
        }
    }

    public enum TaskType
    {
        FETCH_DATA,
        CANCEL_FETCH_DATA,
        FETCH_PLACEHOLDERS,
        CANCEL_FETCH_PLACEHOLDERS,
        WATCHER_TASK
    }

    public class RunningTask
    {
        public TaskType type;
        public Callback? callback;
        public Task task;
        public CancellationTokenSource taskCancelation;
        public string opID;

        public RunningTask(TaskType type, Task task, CancellationTokenSource taskCancelation, string opID, Callback? callback = null)
        {
            this.type = type;
            this.callback = callback;
            this.task = task;
            this.taskCancelation = taskCancelation;
            this.opID = opID;
        }

        public void Cancel()
        {
            taskCancelation.Cancel();
        }
    }
}
