using System;
using System.ServiceProcess;
using SharpDaemon.Server;

namespace SharpDaemon.Service
{
    class Program : ServiceBase
    {
        public static readonly string NAME = "DaemonManager";

        private Listener listener;

        public Program()
        {
            this.ServiceName = NAME;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            listener = new Listener();
        }

        protected override void OnStop()
        {
            base.OnStop();

            Tools.Try(listener.Dispose);
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Tools.ExceptionHandler;

            ServiceBase.Run(new Program());
        }
    }
}