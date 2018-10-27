using System;

namespace Shipwreck.ClickOnce.Manifest.Demo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var settings = new ApplicationManifestSettings()
            {
                FromDirectory = "../../../../Shipwreck.ClickOnce.Manifest.TestApp/bin/Release",
                ToDirectory = "../../../publish/Application Files/TestApp_1_2_3_4",
                Version = new Version(1, 2, 3, 4),
                DeleteDirectory = true,
                Overwrite = true
            };

            settings.Include.Add("!**/*.xml");
            settings.Include.Add("!System.Data.SQLite.dll.config");

            new ApplicationManifestGenerator(settings).Generate();

            Console.ReadKey();
        }
    }
}