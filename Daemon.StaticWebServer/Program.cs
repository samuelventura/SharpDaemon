using System;
using Nancy.Hosting.Self;
using Nancy.Conventions;
using Nancy;

namespace SharpDaemon.Test.Daemon
{
    class CliArgs
    {
        public string Endpoint { get; set; }
        public string Root { get; set; }
        public bool Test { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Tools.ExceptionHandler;

            var cargs = new CliArgs();

            Stdio.WriteLine("Args {0}", string.Join(" ", args));

            var id = args[0]; //string matching [a-zA_Z][a-zA_Z0-9_]*

            Stdio.WriteLine("Arg 0 {0}", id);

            for (var i = 1; i < args.Length; i++)
            {
                Stdio.WriteLine("Arg {0} {1}", i, args[i]);

                Tools.SetProperty(cargs, args[i]);
            }

            Tools.Assert(!string.IsNullOrWhiteSpace(cargs.Endpoint), "Missing endpoint");
            Tools.Assert(!string.IsNullOrWhiteSpace(cargs.Root), "Missing root");

            using (var host = cargs.Test ? CreateForTesting(cargs) : CreateAsAdmin(cargs))
            {
                host.Start();
                Stdio.WriteLine("Serving at {0}", Uri(cargs.Endpoint));

                var line = Stdio.ReadLine();
                while (line != null) line = Stdio.ReadLine();
            }

            Stdio.WriteLine("Stdin closed");

            Environment.Exit(0);
        }

        static Uri Uri(string endpoint) => new Uri(string.Format("http://{0}", endpoint));

        public static NancyHost CreateForTesting(CliArgs cargs)
        {
            var uri = Uri(cargs.Endpoint);
            return new NancyHost(new Bootstrapper() { Root = cargs.Root }, uri);
        }

        public static NancyHost CreateAsAdmin(CliArgs cargs)
        {
            var uri = Uri(cargs.Endpoint);
            var conf = new HostConfiguration();
            conf.RewriteLocalhost = true;
            conf.UrlReservations.CreateAutomatically = true; //needs elevation
            return new NancyHost(new Bootstrapper() { Root = cargs.Root }, conf, uri);
        }

        class Bootstrapper : DefaultNancyBootstrapper
        {
            public string Root;

            protected override void ConfigureConventions(NancyConventions conventions)
            {
                base.ConfigureConventions(conventions);

                //relative to root path configured below
                conventions.StaticContentsConventions.Add(
                    StaticContentConventionBuilder.AddDirectory("/", ".")
                );
            }

            protected override IRootPathProvider RootPathProvider
            {
                get { return new CustomRootPathProvider { Root = Root }; }
            }
        }

        class CustomRootPathProvider : IRootPathProvider
        {
            public string Root;

            //must be an absolute root path
            public string GetRootPath()
            {
                return Root;
            }
        }
    }
}