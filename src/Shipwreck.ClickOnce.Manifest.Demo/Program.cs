using System;

namespace Shipwreck.ClickOnce.Manifest.Demo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var af = "../../../publish/Application Files/TestApp_1_2_3_4";
            {
                var settings = new ApplicationManifestSettings()
                {
                    FromDirectory = "../../../../Shipwreck.ClickOnce.Manifest.TestApp/bin/Release",
                    ToDirectory = af,
                    Version = new Version(1, 2, 3, 4),
                    DeleteDirectory = true,
                    Overwrite = true
                };

                settings.Include.Add("!**/*.xml");
                settings.Include.Add("!System.Data.SQLite.dll.config");

                new ApplicationManifestGenerator(settings).Generate();
            }
            {
                var settings = new DeploymentManifestSettings()
                {
                    FromDirectory = af,
                    ToDirectory = "../../../publish/",
                    Overwrite = true,

                    ApplicationName = "TestApp",

                    Publisher = "Test Publisher",
                    SuiteName = "Test Suite",
                    Product = "Test Product",
                    SupportUrl = "http://never.shipwreck.jp/support",
                    ErrorReportUrl = "http://never.shipwreck.jp/errorReport"
                };

                new DeploymentManifestGenerator(settings).Generate();
            }
            Console.ReadKey();
        }
    }
}