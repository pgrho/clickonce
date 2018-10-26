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
                FromDirectory = dir
            };

            settings.Include.Add("!**/*.xml");

            ApplicationManifestGenerator.Generate(settings);

            Console.ReadKey();
        }
    }
}
