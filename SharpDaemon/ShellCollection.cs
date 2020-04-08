using System;
using System.Text;
using System.Collections.Generic;

namespace SharpDaemon
{
    public class Shell : IShell
    {
        private readonly List<IShell> shells;

        public Shell(List<IShell> shells)
        {
            this.shells = shells;
        }

        public void Execute(IStream stream, params string[] tokens)
        {
            var list = new List<string>(tokens);
            foreach (var shell in shells)
            {
                //explicit try to bubble up io exceptions
                try { shell.Execute(stream, list.ToArray()); }
                catch (Exception ex) { stream.HandleException(ex); }
            }
        }

        public static void ParseAndExecute(IShell shell, IStream stream, string line)
        {
            try
            {
                //explicit try to bubble up io exceptions
                var tokens = Tokens(line); //throws parse exceptions
                if (tokens != null && tokens.Length > 0) shell.Execute(stream, tokens);
            }
            catch (Exception ex) { stream.HandleException(ex); }
        }

        private static string[] Tokens(string line, char quote = '`')
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
                    else throw ExceptionTools.Make("Unclosed quote {0}", quote);
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