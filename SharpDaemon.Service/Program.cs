﻿using System;
using System.ServiceProcess;
using SharpDaemon.Server;

namespace SharpDaemon.Service
{
    class Program : ServiceBase
    {
        public static readonly string NAME = "DaemonManager";

        private Instance instance;

        public Program()
        {
            this.ServiceName = NAME;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            var outputs = new Outputs();
            var cargs = Launcher.ParseCli(outputs, args);
            instance = Launcher.Launch(outputs, cargs);
        }

        protected override void OnStop()
        {
            base.OnStop();

            Tools.Try(instance.Dispose);
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Tools.ExceptionHandler;

            ServiceBase.Run(new Program());
        }
    }
}