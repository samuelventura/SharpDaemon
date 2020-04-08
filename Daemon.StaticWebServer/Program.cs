using System;
using Nancy.Hosting.Self;
using Nancy.Conventions;
using Nancy;
using SharpDaemon;

namespace Daemon.StaticWebServer
{
    class Program
    {
        class Config
        {
            public string Id { get; set; }
            public string Root { get; set; }
            public string EndPoint { get; set; }
        }

        static void Main(string[] args)
        {
            ExceptionTools.SetupDefaultHandler();

            var config = new Config();

            Stdio.WriteLine("Args {0} {1}", args.Length, string.Join(" ", args));

            for (var i = 0; i < args.Length; i++)
            {
                Stdio.WriteLine("Arg {0} {1}", i, args[i]);

                ConfigTools.SetProperty(config, args[i]);
            }

            AssertTools.NotEmpty(config.EndPoint, "Missing endpoint");
            AssertTools.NotEmpty(config.Root, "Missing root");

            var uri = new Uri(string.Format("http://{0}", config.EndPoint));
            var host = new NancyHost(new Bootstrapper() { Root = config.Root }, uri);
            using (host)
            {
                host.Start();
                Stdio.WriteLine("Serving at {0}", uri);

                var line = Stdio.ReadLine();
                while (line != null) line = Stdio.ReadLine();
            }

            Stdio.WriteLine("Stdin closed");

            Environment.Exit(0);
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