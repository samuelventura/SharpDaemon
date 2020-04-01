﻿using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpDaemon
{
    public static class Tools
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);

        public static void MakeNotInheritable(this TcpListener socket)
        {
            const uint HANDLE_FLAG_INHERIT = 1;
            var handle = socket.Server.Handle;
            SetHandleInformation(handle, HANDLE_FLAG_INHERIT, 0);
        }

        public static string Compact(DateTime dt) => dt.ToString("yyyyMMdd_HHmmss_fff");
        public static string Format(DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
        public static string Format(string format, params object[] args)
        {
            var text = format;
            if (args.Length > 0) text = string.Format(format, args);
            return text;
        }

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
            return new Exception(Format(format, args));
        }

        public static string Relative(string format, params object[] args)
        {
            var child = Format(format, args);
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

        public static void SetProperty(object target, string line)
        {
            var parts = line.Split(new char[] { '=' });
            if (parts.Length != 2) throw Make("Expected 2 parts in {0}", Readable(line));
            var propertyName = parts[0];
            var propertyValue = parts[1];
            var property = target.GetType().GetProperty(propertyName);
            if (property == null) throw Make("Property not found {0}", Readable(propertyName));
            var value = Convert.ChangeType(propertyValue, property.PropertyType);
            property.SetValue(target, value, null);
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
