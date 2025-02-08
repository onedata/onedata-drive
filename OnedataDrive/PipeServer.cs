using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive
{
    public class PipeServer
    {
        public bool running { get; private set; } = false;
        public string pipeName { get; private set; } = "";
        private CancellationTokenSource cts;
        private Task serverTask;
        
        public void Start(string pipeName)
        {
            if (running)
            {
                return;
            }
            cts = new();
            this.pipeName = pipeName;
            this.serverTask = Task.Run(() => StartServer(cts.Token), cts.Token);
            this.running = true;
            Debug.Print("PIPE SERVER: running");
        }

        public void Stop()
        {
            if (!running)
            {
                return;
            }
            cts.Cancel();
            this.serverTask.Wait();
            this.running = false;
            Debug.Print("PIPE SERVER: stopped");
        }

        public async void StopAsync()
        {
            await Task.Run(() => Stop());
        }

        private void StartServer(CancellationToken cToken)
        {
            while (true)
            {
                using (NamedPipeServerStream server = new(pipeName, PipeDirection.InOut))
                {
                    StreamReader reader = new StreamReader(server);
                    Debug.Print("PIPE SERVER: ready for client");
                    Task waitForConnection = server.WaitForConnectionAsync();
                    while (!waitForConnection.IsCompleted)
                    {
                        if (cToken.IsCancellationRequested)
                        {
                            server.Close();
                            return;
                        }
                        Task.Delay(10).Wait();
                    }

                    Debug.Print("PIPE SERVER: listening for client`s messages");
                    while (server.IsConnected)
                    {
                        if (reader.EndOfStream)
                        {
                            Task.Delay(100).Wait();
                            continue;
                        }
                        else
                        {
                            string received = reader.ReadLine() ?? "";
                            Debug.Print("RECEIVED: " + received);
                        }
                        if (cToken.IsCancellationRequested)
                        {
                            server.Close();
                            return;
                        }
                    }
                    Debug.Print("PIPE SERVER: client disconnected");
                }
            }
        }
    }
}
