using System;

namespace Shipwreck.ClickOnce.Manifest.Demo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var dir = new Uri(new Uri(typeof(Program).Assembly.Location), "../../../../Shipwreck.ClickOnce.Manifest.TestApp/bin/Release").LocalPath;

            Console.WriteLine(dir);

            var settings = new ApplicationManifestSettings()
            {
                FromDirectory = dir,
                ToDirectory = "../../../publish",
                Version = new Version(1, 2, 3, 4)
            };

            settings.Include.Add("!**/*.xml");
            settings.Include.Add("!System.Data.SQLite.dll.config");

            new ApplicationManifestGenerator(settings).Generate();

            Console.ReadKey();
        }
    }
}
