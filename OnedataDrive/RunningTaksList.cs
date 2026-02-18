using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive
{
    public class RunningTaksList : IDisposable
    {
        private CancellationTokenSource masterCTS;
        public ThreadSafeList<RunningTask2> runningTasks { get; private set; }
        public Task listCleaner { get; private set; }
        public const int CLEANER_PERIOD_SECONDS = 10;

        public RunningTaksList()
        {
            this.masterCTS = new CancellationTokenSource(); 
            this.runningTasks = new ThreadSafeList<RunningTask2>();
            this.listCleaner = CleanerTask(masterCTS.Token, CLEANER_PERIOD_SECONDS);
        }

        public RunningTask2 AddTask(Func<CancellationToken, Task> funcToStart, TaskType type, string opID)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(masterCTS.Token);
            Task newTask = funcToStart.Invoke(cts.Token);
            RunningTask2 runningTask = new RunningTask2(type, newTask, cts, opID);
            this.runningTasks.Add(runningTask);
            return runningTask;
        }

        public void Dispose() 
        {
            if (!masterCTS.IsCancellationRequested)
            {
                masterCTS.Cancel();
            }
        }

        private async Task CleanerTask(CancellationToken token, int periodSeconds)
        {
            while (!token.IsCancellationRequested)
            {
                runningTasks.RemoveAll(rt => rt.task.IsCompleted);
                token.WaitHandle.WaitOne(periodSeconds * 1000);
            }
        }
    }

    public enum TaskType
    {
        FETCH_DATA,
        CANCEL_FETCH_DATA,
        FETCH_PLACEHOLDERS,
        CANCEL_FETCH_PLACEHOLDERS,
    }

    public class RunningTask2
    {
        public TaskType type;
        public FetchDataCallback? callback;
        public Task task;
        public CancellationTokenSource taskCancelation;
        public string opID;

        public RunningTask2(TaskType type, Task task, CancellationTokenSource taskCancelation, string opID, FetchDataCallback? callback = null)
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
