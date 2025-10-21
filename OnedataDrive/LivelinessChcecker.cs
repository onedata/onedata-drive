using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive
{
    public class LivelinessChcecker : IDisposable
    {
        private Task _checkingTask;
        private bool alive;
        private CancellationTokenSource cts;
        private Action callback;
        public int checkInterval { get; private set; }
        public bool disposed { get; private set; }
        public bool turnOffWhenDead { get; private set; }
        public LivelinessChcecker(uint checkInterval, Action? callback = null, bool turnOffWhenDead = true)
        {
            this.disposed = false;
            this.cts = new CancellationTokenSource();
            this.alive = true;
            this.callback = callback ?? DefaultCallback;
            this._checkingTask = Task.CompletedTask;
            this.checkInterval = (int)checkInterval;
            this.turnOffWhenDead = turnOffWhenDead;
        }

        public void Start()
        {
            if (disposed)
            {
                return;
            }
            _checkingTask = CheckingLoop(cts.Token);
        }

        public bool IsChecking()
        {
            if (disposed)
            {
                return false;
            }
            return !_checkingTask.IsCompleted;
        }
        public void IamAlive()
        {
            alive = true;
        }

        public void Dispose()
        {
            cts.Cancel();
        }

        private void DefaultCallback()
        {
            Debug.Print("Liveliness check failed.");
        }

        private async Task CheckingLoop(CancellationToken token)
        {
            do
            {
                await Task.Delay(checkInterval, token);
                if (!alive)
                {
                    callback.Invoke();
                    if (turnOffWhenDead)
                    {
                        return;
                    }
                }
                alive = false;
            } while (!token.IsCancellationRequested);
        }
    }
}
