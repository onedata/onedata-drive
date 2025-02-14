using OnedataDrive.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                        Debug.Print("1");
                        if (cToken.IsCancellationRequested)
                        {
                            server.Close();
                            return;
                        }
                        Debug.Print("2");
                        if (!readerTask.IsCompleted)
                        {
                            Task.Delay(100).Wait();
                            Debug.Print("4");
                            continue;
                        }
                        else
                        {
                            string received = readerTask.Result ?? "";
                            Debug.Print("OK: " + received);
                            string response = HandleCommand(received);
                            writer.WriteLine(response);
                            writer.Flush();
                            readerTask = reader.ReadLineAsync(cToken);
                        }
                        Debug.Print("3");
                    }
                    Debug.Print("PIPE SERVER: client disconnected");
                }
            }
        }

        private string HandleCommand(string msg)
        {
            if (msg.Length == 0)
            {
                throw new Exception();
            }
            Commands command;
            List<string> content;
            try
            {
                string[] rawContent = msg.Split("|");
                command = (Commands)Enum.Parse(typeof(Commands), rawContent[0]);
                content = rawContent.Skip(1).ToList();
            }
            catch (Exception e)
            {
                Debug.Print($"Invalid command {msg}");
                return new PipeCommand(Commands.FAIL).ToString();
            }
            
            string response = "";
            
            switch (command)
            {
                case Commands.SEND_ROOT:
                    Debug.Print("Send root");
                    response = new PipeCommand(Commands.OK, [CloudSync.configuration.root_path]).ToString();
                    break;
                case Commands.REQUEST_REFRESH:
                    // do something
                    Debug.Print("Selected paths");
                    content.ForEach(x => Debug.Print(x));
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
