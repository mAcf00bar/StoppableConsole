using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;

using Serilog;

namespace StoppableConsole
{
    public class Program
    {
        private static string PipeName => GetPipeName();
        private static bool StopRequested { get; set; } = false;
        private static bool StopIsNotRequested => !StopRequested;

        private static ILogger _logger = Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        public static void Main(string[] args)
        {
            // first instance check
            using (var mutex = new Mutex(true, PipeName, out bool isFirstInstance))
            {
                if (isFirstInstance)
                {
                    _logger.Information("This is the first instance.");
                    using (var serverPipe = new NamedPipeServerStream(PipeName))
                    {
                        // Start a thread that listens for messages from other instances
                        var listenerThread = new Thread(ListenerThread);
                        listenerThread.Start(serverPipe);

                        LongRunningTask();

                        // Wait for the listener thread to finish
                        listenerThread.Join();
                    }
                }
                else
                {
                    //There is already another instance running so send stop command if the command line argument is set
                    if (args.Length >= 1 && args[0] == "stop")
                        SendStopCommand();
                }

                // Dispose of any resources and exit
                _logger.Information("Exiting...");
                Log.CloseAndFlush();
            }
        }

        private static void SendStopCommand()
        {
            _logger.Information("This is not the first instance.");
            using (var client = new NamedPipeClientStream(PipeName))
            {
                client.Connect();
                var buffer = new byte[] { (byte)CommandType.Stop }; // 1 -> stop, anything else -> go on
                client.Write(buffer, 0, buffer.Length);
                client.Flush();
            }
        }

        // A method that listens for messages from other instances via named pipes
        private static void ListenerThread(object state)
        {
            // listen only to StopCommands
            
                var server = (NamedPipeServerStream)state;
                server.WaitForConnection();

                var buffer = new byte[1];
                server.Read(buffer, 0, buffer.Length);
                if ((CommandType)buffer[0] == CommandType.Stop)
                {
                    // This is a stop command, set the StopRequested flag to true and log a message
                    StopRequested = true;
                    _logger.Information("Received stop command from another instance.");
                }

                // Close the server stream and exit
                server.Close();
            
        }

        // simulates a long running background task that can be stopped by setting the flag StopRequested
        private static void LongRunningTask()
        {
            while (StopIsNotRequested)
            {
                // In each iteration, log a message and sleep for one second
                _logger.Information("Running background task... {CurrentTime}", DateTime.Now);
                Thread.Sleep(1000);
            }

            _logger.Information("Stopping background task...");
        }

        public static string GetPipeName()
        {
            var appName = Process.GetCurrentProcess().ProcessName;
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return $"{appName}_{appDirectory.GetHashCode()}";
        }
    }
}