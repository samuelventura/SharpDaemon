using System;
using System.ServiceProcess;

namespace SharpDaemon.Service
{
    class Program : ServiceBase
    {
        //name seems to be taken from `sc create SERVICENAME` command
        public static readonly string NAME = "Daemon Manager";

        private DaemonProcess process;

        public Program()
        {
            this.ServiceName = NAME;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            process = new DaemonProcess(new DaemonProcess.Args
            {
                Executable = ExecutableTools.Relative("SharpDaemon.Server.exe"),
                Arguments = "Daemon=True",
            });
        }

        protected override void OnStop()
        {
            base.OnStop();

            ExceptionTools.Try(process.Dispose);
        }

        static void Main(string[] args)
        {
            ProgramTools.Setup();

            ServiceBase.Run(new Program());
        }
    }
}