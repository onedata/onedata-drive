using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive
{
    public class RunningTaksList
    {
        public CancellationTokenSource masterCTS { get; private set; }
        public List<RunningTask2> runningTasks { get; private set; }

        public RunningTaksList()
        {
            this.masterCTS = new CancellationTokenSource(); 
            this.runningTasks = new List<RunningTask2>();
        }

        public void Dispose() 
        {
            
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
