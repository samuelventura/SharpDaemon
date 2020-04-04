using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace SharpDaemon
{
    public interface IShell
    {
        void Execute(Shell.IO io, params string[] tokens);
    }

    public class Shell : IShell
    {
        private readonly List<IShell> shells;

        public Shell(List<IShell> shells)
        {
            this.shells = shells;
        }

        public void Execute(Shell.IO io, params string[] tokens)
        {
            var list = new List<string>(tokens);
            foreach (var shell in shells)
            {
                //explicit try to bubble up io exceptions
                try { shell.Execute(io, list.ToArray()); }
                catch (Exception ex) { io.OnException(ex); }
            }
        }

        public void ParseAndExecute(Shell.IO io, string line)
        {
            try
            {
                //explicit try to bubble up io exceptions
                var tokens = Tokens(line); //throws on format errors
                if (tokens != null && tokens.Length > 0) Execute(io, tokens);
            }
            catch (Exception ex) { io.OnException(ex); }
        }

        public interface IO : IOutput
        {
            string ReadLine();
            string ReadLine(out bool eof);
        }

        private string[] Tokens(string line, char quote = '`')
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
                    else throw Tools.Make("Unclosed quote {0}", quote);
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

    public class ShellFactory
    {
        private readonly List<IShell> shells;

        public ShellFactory()
        {
            shells = new List<IShell>();
        }

        public void Add(IShell shell)
        {
            shells.Add(shell);
        }

        public Shell Create()
        {
            return new Shell(new List<IShell>(shells));
        }
    }

    public class StreamIO : Shell.IO, IDisposable
    {
        private readonly LockedQueue<string> queue = new LockedQueue<string>();
        private readonly IOutput output;
        private readonly TextReader reader;
        private readonly Runner runner;

        public StreamIO(IOutput output, TextReader reader)
        {
            this.output = output;
            this.reader = reader;
            runner = new Runner();
            runner.Run(ReadLoop);
            runner.Run(() => queue.Push(Environment.NewLine));
        }

        public void Dispose()
        {
            Tools.Try(reader.Dispose);
            Tools.Try(runner.Dispose);
        }

        public string ReadLine()
        {
            var line = queue.Pop(1, null);
            while (line == null) line = queue.Pop(1, null);
            if (line == Environment.NewLine) return null;
            return line;
        }

        public string ReadLine(out bool eof)
        {
            eof = false;
            var line = queue.Pop(1, null);
            if (line != Environment.NewLine) return line;
            eof = true;
            return null;
        }

        public void OnException(Exception ex)
        {
            output.OnException(ex);
        }

        public void WriteLine(string format, params object[] args)
        {
            output.WriteLine(format, args);
        }

        private void ReadLoop()
        {
            var line = reader.ReadLine();
            while (line != null)
            {
                queue.Push(line);
                line = reader.ReadLine();
            }
        }
    }
}