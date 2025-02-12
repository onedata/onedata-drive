using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

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
                            string response = HandleCommand(received);
                            writer.WriteLine(response);
                            writer.Flush();
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

        public enum Commands
        {
            SEND_ROOT,
            SELECTED_PATHS,
            RECEIVED,
            FAIL
        }

        public string CreateCommandMsg(Commands command, string[]? content = null)
        {
            content = content ?? Array.Empty<string>();
            string msg = Commands.SELECTED_PATHS.ToString();
            if (content.Length != 0)
            {
                msg += "|" + string.Join("|", content);
            }
            return msg;
        }

        private string HandleCommand(string msg)
        {
            if (msg.Length == 0)
            {
                throw new Exception();
            }
            Commands command;
            try
            {
                command = (Commands)Int32.Parse(msg.Split("|").First());
            }
            catch (Exception e)
            {
                Debug.Print("Invalid command");
                throw new Exception("Invalid command", e);
            }
            
            string response = "";
            
            switch (command)
            {
                case Commands.SEND_ROOT:
                    // do something
                    response = CreateCommandMsg(Commands.RECEIVED);
                    break;
                case Commands.SELECTED_PATHS:
                    // do something
                    response = CreateCommandMsg(Commands.RECEIVED);
                    break;
                default:
                    response = CreateCommandMsg(Commands.FAIL);
                    break;
            }
            Debug.Print("PIPE SERVER msg received");
            return response;
        }
    }
}
