using System;
using System.IO;

namespace SharpDaemon.Server
{
    public class Instance : IDisposable
    {
        private readonly TextWriter writer;
        private readonly Listener listener;
        private readonly bool stdout;
        private readonly int port;

        public class Args
        {
            public int RestartDelay { get; set; }
            public bool Stdout { get; set; }
            public int TcpPort { get; set; }
            public string DbPath { get; set; }
            public string LogPath { get; set; }
            public string Downloads { get; set; }
        }

        public Instance(Args args)
        {
            stdout = args.Stdout;
            using (var disposer = new Disposer())
            {
                //acts like mutex for the workspace
                writer = new StreamWriter(args.LogPath);
                disposer.Push(writer);
                Log("DbPath {0}", args.DbPath);
                Log("LogPath {0}", args.LogPath);
                Log("Downloads {0}", args.Downloads);
                Log("RestartDelay {0}", args.RestartDelay);
                Log("Stdout {0}", args.Stdout);
                Log("TcpPort {0}", args.TcpPort);
                var controller = new Controller(new Controller.Args
                {
                    ExceptionHandler = OnException,
                    DaemonLogger = OnDaemonLog,
                    RestartDelay = args.RestartDelay,
                });
                disposer.Push(controller);
                var manager = new Manager(new Manager.Args
                {
                    ExceptionHandler = OnException,
                    DatabasePath = args.DbPath,
                    Controller = controller,
                });
                disposer.Push(manager);
                var factory = new InteractiveFactory(new InteractiveFactory.Args
                {
                    ExceptionHandler = OnException,
                    Manager = manager,
                });
                disposer.Push(factory);
                listener = new Listener(new Listener.Args
                {
                    ExceptionHandler = OnException,
                    ShellFactory = factory,
                    TcpPort = args.TcpPort,
                    Output = Output,
                });
                disposer.Push(listener);
                port = listener.Port;
                disposer.Clear();
            }
        }

        public int Port { get { return port; } }

        public void Dispose()
        {
            Tools.Try(listener.Dispose, OnException);
            Tools.Try(writer.Dispose, OnException);
        }

        //called from many threads
        public void Log(string format, params object[] args)
        {
            var text = format;
            if (args.Length > 0) text = string.Format(format, args);
            WriteLine("{0} {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                text);
        }

        //called from many threads
        private void OnDaemonLog(DaemonLog log)
        {
            WriteLine("{0} {1} {2} {3} {4} {5}",
                log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                log.Level, log.Uid, log.Name, log.Pid, log.Message);
        }

        //called from many threads
        private void OnException(Exception ex)
        {
            Tools.Try(() => Tools.Dump(ex));
            WriteLine("{0} {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                ex.ToString());
        }

        //called from many threads
        private void WriteLine(string format, params object[] args)
        {
            var text = format;
            if (args.Length > 0) text = string.Format(format, args);
            if (stdout) Tools.Try(() => Stdio.WriteLine(text));
            Tools.Try(() =>
            {
                lock (writer)
                {
                    writer.WriteLine(text);
                    writer.Flush();
                }
            });
        }

        //called from many threads
        private void Output(string line) => Log(line);
    }
}