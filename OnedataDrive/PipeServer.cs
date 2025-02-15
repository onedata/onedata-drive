using System.Diagnostics;
using System.IO.Pipes;

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
                    StreamWriter writer = new StreamWriter(server);
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
                    ValueTask<string?> readerTask = reader.ReadLineAsync(cToken);
                    while (server.IsConnected)
                    {
                        if (cToken.IsCancellationRequested)
                        {
                            server.Close();
                            return;
                        }
                        if (!readerTask.IsCompleted)
                        {
                            Task.Delay(100).Wait();
                            continue;
                        }
                        else
                        {
                            string received = readerTask.Result ?? "";
                            Debug.Print("PIPE SERVER received: " + received);
                            string response = HandleCommand(received);
                            writer.WriteLine(response);
                            writer.Flush();
                            readerTask = reader.ReadLineAsync(cToken);
                        }
                    }
                    Debug.Print("PIPE SERVER: client disconnected");
                }
            }
        }

        private string HandleCommand(string msg)
        {
            PipeCommand received = new(msg ?? "");

            string response = "";

            switch (received.command)
            {
                case Commands.SEND_ROOT:
                    Debug.Print("Send root");
                    response = new PipeCommand(Commands.OK, [CloudSync.configuration.root_path]).ToString();
                    break;
                case Commands.REFRESH_SPACE:
                    // do something
                    Debug.Print("Refresh space");
                    received.payload.ForEach(x => Debug.Print($"Path: {x}"));
                    response = new PipeCommand(Commands.OK).ToString();
                    break;
                case Commands.REFRESH_FILES:
                    Debug.Print("Refresh files");
                    received.payload.ForEach(x => Debug.Print($"Path: {x}"));
                    response = new PipeCommand(Commands.OK).ToString();
                    break;
                case Commands.REFRESH_FOLDER:
                    Debug.Print("Refresh folder");
                    received.payload.ForEach(x => Debug.Print($"Path: {x}"));
                    response = new PipeCommand(Commands.OK).ToString();
                    break;
                default:
                    response = new PipeCommand(Commands.FAIL).ToString();
                    Debug.Print("Default");
                    break;
            }
            Debug.Print("PIPE SERVER msg received");
            return response;
        }
    }
}
