﻿using System;
using System.Threading;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public class Controller : IDisposable
    {
        private readonly int delay;
        private readonly Runner runner;
        private readonly Action<DaemonLog> logger;
        private readonly Action<Exception> handler;
        private readonly Dictionary<string, DaemonRT> daemons;

        public class Args
        {
            public int RestartDelay { get; set; }
            public Action<DaemonLog> DaemonLogger { get; set; }
            public Action<Exception> ExceptionHandler { get; set; }
        }

        public Controller(Args args = null)
        {
            args = args ?? new Args();
            delay = args.RestartDelay;
            logger = args.DaemonLogger;
            handler = args.ExceptionHandler;
            daemons = new Dictionary<string, DaemonRT>();
            runner = new Runner(new Runner.Args { ExceptionHandler = handler });
        }

        public void Start(DaemonDto dto, Callback callback = null)
        {
            runner.Run(() =>
            {
                Tools.Assert(daemons.ContainsKey(dto.Id), "Daemon {0} already started", dto.Id);
                DoStart(dto);
            }, callback);
        }

        public void Stop(string id, Callback callback = null)
        {
            runner.Run(() =>
            {
                Tools.Assert(!daemons.ContainsKey(id), "Unknown daemon {0} to stop", id);
                DoStop(id);
            }, callback);
        }

        public void Dispose()
        {
            runner.Dispose(() =>
            {
                foreach (var rt in daemons.Values)
                {
                    Tools.Try(rt.Process.Dispose, handler);
                    Tools.Try(rt.Runner.Dispose, handler);
                }
                daemons.Clear();
            });
        }

        private void Restart(DaemonDto dto)
        {
            Thread.Sleep(delay);
            runner.Run(() =>
            {
                var id = dto.Id;
                Tools.Assert(!daemons.ContainsKey(id), "Unknown daemon {0} to restart", id);
                DoStop(id);
                DoStart(dto);
            });
        }

        private void DoStart(DaemonDto dto)
        {
            var rt = new DaemonRT()
            {
                Id = dto.Id,
                Path = dto.Path,
                Args = dto.Args,
                Logger = logger,
                Handler = handler,
                Restart = Restart,
                Process = new DaemonProcess(new DaemonProcess.Args
                {
                    Executable = dto.Path,
                    Arguments = dto.Args,
                    ExceptionHandler = handler,
                }),
                Runner = new Runner(new Runner.Args { ExceptionHandler = handler }),
            };
            daemons[dto.Id] = rt;
        }

        private void DoStop(string id)
        {
            var rt = daemons[id];
            daemons.Remove(id);
            Tools.Try(rt.Process.Dispose, handler);
            Tools.Try(rt.Runner.Dispose, handler);
        }
    }

    public class DaemonRT : DaemonDto
    {
        public Action<DaemonDto> Restart { get; set; }
        public Action<DaemonLog> Logger { get; set; }
        public Action<Exception> Handler { get; set; }
        public Runner Runner { get; set; }
        public DaemonProcess Process { get; set; }

        public void ReadLoop()
        {
            Tools.Try(TryLoop, Handler);
            Restart(this);
        }

        private void TryLoop()
        {
            var line = Process.ReadLine();
            while (!string.IsNullOrWhiteSpace(line))
            {
                if (line.StartsWith("#"))
                {
                    Tools.Try(() => TryLog(line), Handler);
                }
                line = Process.ReadLine();
            }
        }

        private void TryLog(string line)
        {
            var log = new DaemonLog()
            {
                Uid = Id,
                Pid = Process.Id,
                Name = Process.Name,
            };
            Log.Parse(log, line);
            Logger?.Invoke(log);
        }
    }

    public class DaemonLog : Log
    {
        public int Pid { get; set; }
        public string Uid { get; set; }
        public string Name { get; set; }
    }
}
