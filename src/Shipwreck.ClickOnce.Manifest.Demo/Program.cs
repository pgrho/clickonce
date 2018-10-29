using System;

namespace Shipwreck.ClickOnce.Manifest.Demo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var src = "../../../../Shipwreck.ClickOnce.Manifest.TestApp/bin/Release";
            var applicationFiles = "../../../publish/Application Files/TestApp_1_2_3_4";

            //var pfx = "../../../../Shipwreck.ClickOnce.Manifest.TestApp/TestApp.pfx";
            //var pw = "password";
            string pfx = null, pw = null;
            {
                var settings = new ApplicationManifestSettings()
                {
                    FromDirectory = src,
                    ToDirectory = applicationFiles,
                    Version = new Version(1, 2, 3, 4),
                    DeleteDirectory = true,
                    Overwrite = true,

                    CertificateFileName = pfx,
                    CertificatePassword = pw,
                };

                settings.Include.Add("!**/*.xml");
                settings.Include.Add("!System.Data.SQLite.dll.config");

                new ApplicationManifestGenerator(settings).Generate();
            }
            {
                var settings = new DeploymentManifestSettings()
                {
                    FromDirectory = applicationFiles,
                    ToDirectory = "../../../publish/",
                    Overwrite = true,

                    ApplicationName = "TestApp",

                    Publisher = "Test Publisher",
                    SuiteName = "Test Suite",
                    Product = "Test Product",
                    SupportUrl = "http://never.shipwreck.jp/support",
                    ErrorReportUrl = "http://never.shipwreck.jp/errorReport",

                    Install = true,
                    CreateDesktopShortcut = true,

                    CertificateFileName = pfx,
                    CertificatePassword = pw,
                };

                new DeploymentManifestGenerator(settings).Generate();
            }
            {
                var settings = new PublishSettings()
                {
                    FromDirectory = src,
                    ToDirectory = "../../../publish/auto/",

                    Version = new Version(2, 3, 4, 5),

                    DeleteDirectory = true,
                    Overwrite = true,

                    ApplicationName = "TestApp2",

                    Publisher = "Test Publisher",
                    SuiteName = "Test Suite",
                    Product = "Test Product",
                    SupportUrl = "http://never.shipwreck.jp/support",
                    ErrorReportUrl = "http://never.shipwreck.jp/errorReport",

                    Install = true,
                    CreateDesktopShortcut = true,

                    CertificateFileName = pfx,
                    CertificatePassword = pw,
                };
                settings.Include.Add("!**/*.xml");
                settings.Include.Add("!System.Data.SQLite.dll.config");

                new ApplicationPublisher(settings).Generate();
            }
            Console.ReadKey();
        }
    }
}