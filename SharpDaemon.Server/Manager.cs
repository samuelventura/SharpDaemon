﻿using System;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public partial class Manager : Disposable
    {
        private readonly Dictionary<string, DaemonDto> installed;
        private readonly Dictionary<string, DaemonRT> running;
        private readonly Dictionary<string, DateTime> starting;
        private readonly string database;
        private readonly string root;
        private readonly Runner runner;
        private readonly int delay;

        public class Args
        {
            public string Root { get; set; }
            public string Database { get; set; }
            public int RestartDelay { get; set; }
        }

        public Manager(Args args)
        {
            database = args.Database;
            root = args.Root;
            delay = Math.Max(100, args.RestartDelay);
            starting = new Dictionary<string, DateTime>();
            running = new Dictionary<string, DaemonRT>();
            installed = new Dictionary<string, DaemonDto>();
            runner = new Runner(new Runner.Args
            {
                ThreadName = "Manager",
                IdleAction = IdleLoop,
                IdleMsDelay = 100,
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
                else if (rt.WillRestart())
                {
                    //dispose immediatelly
                    if (!rt.Disposed) ExceptionTools.Try(rt.Dispose);
                }
            }

            //dispose and remove
            foreach (var rt in removing)
            {
                rt.Dispose();
                running.Remove(rt.Id);
                starting.Remove(rt.Id);
                Logger.Trace("Daemon {0} removed", rt.Id);
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
                        starting[dto.Id] = DateTime.Now.AddMilliseconds(delay);
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
            Logger.Trace("Daemon {0} starting {1}...", dto.Id, dto.Info("Path|Args"));
            var rt = new DaemonRT(dto, root, delay);
            running.Add(rt.Id, rt);
            starting.Remove(dto.Id);
            Logger.Trace("Daemon {0} started {1}", rt.Id, rt.Info("Name|Pid"));
        }

        private void ReStartDaemon(DaemonRT rt)
        {
            var dto = rt.Dto;
            rt.UpdateRestart();
            Logger.Trace("Daemon {0} restarting {1}...", dto.Id, dto.Info("Path|Args"));
            rt = new DaemonRT(rt.Dto, root, delay);
            Logger.Trace("Daemon {0} restarted {1}", rt.Id, rt.Info("Name|Pid"));
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