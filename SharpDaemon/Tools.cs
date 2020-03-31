using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace SharpDaemon
{
    public static class Tools
    {
        public static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            ExceptionHandler(args.ExceptionObject as Exception);
        }

        public static void ExceptionHandler(Exception ex)
        {
            Tools.Try(() => Tools.Dump(ex));
            Tools.Try(() => Stdio.WriteLine("!{0}", ex.ToString()));
            Environment.Exit(1);
        }

        public static void Try(Action action, Action<Exception> handler = null)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                if (handler != null)
                {
                    Try(() => { handler(ex); });
                }
            }
        }

        public static Exception Make(string format, params object[] args)
        {
            var line = format;
            if (args.Length > 0) line = string.Format(format, args);
            return new Exception(line);
        }

        public static string Relative(string child)
        {
            var exe = Assembly.GetExecutingAssembly().Location;
            var root = Path.GetDirectoryName(exe);
            return Path.Combine(root, child);
        }

        public static void Dump(Exception ex)
        {
            var folder = Relative("Exceptions");
            Directory.CreateDirectory(folder);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var proc = Process.GetCurrentProcess();
            var file = string.Format("Exception-{0}-{1}-{2:000000}.txt", timestamp, proc.ProcessName, proc.Id);
            var path = Path.Combine(folder, file);
            File.WriteAllText(path, ex.ToString());
        }

        public static string Readable(string text)
        {
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                if (Char.IsControl(c)) sb.Append(((int)c).ToString("X2"));
                else if (Char.IsWhiteSpace(c)) sb.Append(((int)c).ToString("X2"));
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static void Assert(bool condition, string format, params object[] args)
        {
            if (!condition) throw Make(format, args);
        }

        public static string[] Tokens(string line, Output output, char quote = '`')
        {
            var list = new List<string>();
            var sb = new StringBuilder();

            var e = line.GetEnumerator();
            while (e.MoveNext())
            {
                if (char.IsWhiteSpace(e.Current)) continue;
                else if (e.Current == quote)
                {
                    sb.Clear();
                    sb.Append(e.Current);
                    while (e.MoveNext())
                    {
                        sb.Append(e.Current);
                        if (e.Current == quote) break;
                    }
                    var part = sb.ToString();
                    if (part.Length >= 2 && part[0] == quote && part[part.Length - 1] == quote)
                    {
                        list.Add(part.Substring(1, part.Length - 2));
                    }
                    else
                    {
                        output.Output("Error : unclosed quote {0}", quote);
                        return null;
                    }
                }
                else
                {
                    sb.Clear();
                    sb.Append(e.Current);
                    while (e.MoveNext())
                    {
                        if (char.IsWhiteSpace(e.Current)) break;
                        sb.Append(e.Current);
                    }
                    list.Add(sb.ToString());
                }
            }

            return list.ToArray();
        }
    }
}
