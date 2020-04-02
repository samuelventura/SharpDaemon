using System;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public partial class Manager : Disposable
    {
        private readonly Dictionary<string, DaemonDto> installed;
        private readonly Dictionary<string, DaemonRT> running;
        private readonly Dictionary<string, DateTime> starting;
        private readonly string database;
        private readonly string downloads;
        private readonly NamedOutput named;
        private readonly Runner runner;
        private readonly int delay;

        public class Args
        {
            public Output Output { get; set; }
            public string Downloads { get; set; }
            public string Database { get; set; }
            public int RestartDelay { get; set; }
        }

        public Manager(Args args)
        {
            database = args.Database;
            downloads = args.Downloads;
            delay = Math.Max(5, args.RestartDelay);
            named = new NamedOutput("MANAGER", args.Output);
            starting = new Dictionary<string, DateTime>();
            running = new Dictionary<string, DaemonRT>();
            installed = new Dictionary<string, DaemonDto>();
            runner = new Runner(new Runner.Args
            {
                ExceptionHandler = named.OnException,
                ThreadName = "Manager",
                IdleAction = IdleLoop,
                IdleMsDelay = 1,
            });
            runner.Run(ReloadDatabase);
        }

        protected override void Dispose(bool disposed)
        {
            runner.Dispose(() =>
            {
                installed.Clear();
                foreach (var rt in running.Values) rt.Dispose();
                running.Clear();
            });
        }

        private void IdleLoop()
        {
            var removing = new List<DaemonRT>();
            var restarting = new List<DaemonRT>();

            //locate running but not installed
            //or running with different config
            foreach (var rt in running.Values)
            {
                installed.TryGetValue(rt.Id, out var dto);

                if (dto == null || Sid(dto) != Sid(rt.Dto))
                {
                    removing.Add(rt);
                }
                else if (rt.NeedRestart())
                {
                    restarting.Add(rt);
                }
            }

            //dispose and remove
            foreach (var rt in removing)
            {
                rt.Dispose();
                running.Remove(rt.Id);
                starting.Remove(rt.Id);
                named.WriteLine("Daemon {0} removed", rt.Id);
            }

            //launch installed but not running
            foreach (var dto in installed.Values)
            {
                running.TryGetValue(dto.Id, out var rt);

                if (rt == null)
                {
                    var found = starting.TryGetValue(dto.Id, out var dt);
                    if (!found || DateTime.Now > dt)
                    {
                        starting[dto.Id] = DateTime.Now.AddSeconds(delay);
                        runner.Run(() => StartDaemon(dto));
                    }
                }
            }

            //restart if needed
            foreach (var rt in restarting)
            {
                //daemon runner should be idle at this point
                rt.Dispose();
                runner.Run(() => ReStartDaemon(rt));
            }
        }

        private void StartDaemon(DaemonDto dto)
        {
            named.WriteLine("Daemon {0} starting {1}...", dto.Id, dto.Info("Path|Args"));
            var rt = new DaemonRT(dto, downloads, delay, named.Output);
            running.Add(rt.Id, rt);
            starting.Remove(dto.Id);
            named.WriteLine("Daemon {0} started {1}", rt.Id, rt.Info("Name|Pid"));
        }

        private void ReStartDaemon(DaemonRT rt)
        {
            var dto = rt.Dto;
            rt.UpdateRestart();
            named.WriteLine("Daemon {0} restarting {1}...", dto.Id, dto.Info("Path|Args"));
            rt = new DaemonRT(rt.Dto, downloads, delay, named.Output);
            named.WriteLine("Daemon {0} restarted {1}", rt.Id, rt.Info("Name|Pid"));
            running.Remove(rt.Id);
            running.Add(rt.Id, rt);
        }

        private void ReloadDatabase()
        {
            installed.Clear();
            var dtos = Database.List(database);
            foreach (var dto in dtos) installed.Add(dto.Id, dto);
        }

        //string id for equality check
        private string Sid(DaemonDto dto) => dto.Info("Id|Path|Args");
    }
}