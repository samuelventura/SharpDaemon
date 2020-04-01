using System;
using System.Collections.Generic;
using LiteDB;

namespace SharpDaemon.Server
{
    public static class Database
    {
        public static void Save(string path, DaemonDto dto)
        {
            using (var db = new LiteDatabase(path))
            {
                var table = db.GetCollection<DaemonDto>("daemons");
                table.Upsert(dto);
            }
        }

        public static void Remove(string path, string id)
        {
            using (var db = new LiteDatabase(path))
            {
                var table = db.GetCollection<DaemonDto>("daemons");
                table.Delete(id);
            }
        }

        public static List<DaemonDto> List(string path)
        {
            using (var db = new LiteDatabase(path))
            {
                var table = db.GetCollection<DaemonDto>("daemons");
                return new List<DaemonDto>(table.FindAll());
            }
        }
    }

    public class DaemonDto
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public string Args { get; set; }

        public DaemonDto Clone()
        {
            return new DaemonDto
            {
                Id = this.Id,
                Path = this.Path,
                Args = this.Args,
            };
        }
    }
}