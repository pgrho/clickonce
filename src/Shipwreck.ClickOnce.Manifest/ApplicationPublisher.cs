using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public class ApplicationPublisher
    {
        protected internal static readonly TraceSource TraceSource
            = ManifestGenerator.TraceSource;

        public ApplicationPublisher(PublishSettings settings)
            => Settings = settings;

        protected PublishSettings Settings { get; }

        public void Generate()
        {
            var td = new DirectoryInfo(Settings.ToDirectory);

            if (Settings.DeleteDirectory)
            {
                if (td.Exists)
                {
                    TraceSource.TraceInformation("Removing Directory :{0}", td.FullName);
                    td.Delete(true);
                }
            }

            var ams = new ApplicationManifestSettings()
            {
                EntryPoint = Settings.EntryPoint,
                IconFile = Settings.IconFile,
                Version = Settings.Version,

                FromDirectory = Settings.FromDirectory,
                ToDirectory = Settings.ToDirectory?.Length > 0 ? Path.Combine(Settings.ToDirectory, "__application.publish") : "__application.publish",

                Include = Settings.Include,
                Exclude = Settings.Exclude,

                Overwrite = Settings.Overwrite,
                DeleteDirectory = Settings.DeleteDirectory,
            };

            var ag = new ApplicationManifestGenerator(ams);

            var vs = Settings.Version?.ToString()
                     ?? ag.Document.Root.Element(ManifestGenerator.AsmV1 + "assemblyIdentity")?.Attribute("version")?.Value 
                     ?? "1.0.0.0";

            var af = $"Application Files/{Path.GetFileNameWithoutExtension(ag.EntryPointPath)}_{vs.Replace('.', '_')}";
            ams.ToDirectory = Settings.ToDirectory?.Length > 0 ? Path.Combine(Settings.ToDirectory, af) : af;

            ag.Generate();

            var dms = new DeploymentManifestSettings()
            {
                ApplicationName = Settings.ApplicationName,
                Version = Version.Parse(vs),

                CompatibleFrameworks = Settings.CompatibleFrameworks?.Select(e => e.Clone()).ToList(),

                Publisher = Settings.Publisher,
                SuiteName = Settings.SuiteName,
                Product = Settings.Product,
                SupportUrl = Settings.SupportUrl,
                ErrorReportUrl = Settings.ErrorReportUrl,

                Install = Settings.Install,
                CreateDesktopShortcut = Settings.CreateDesktopShortcut,
                CodeBaseFolder = Settings.CodeBaseFolder,

                FromDirectory = ams.ToDirectory,
                ToDirectory = Settings.ToDirectory,

                Overwrite = Settings.Overwrite,
            };

            new DeploymentManifestGenerator(dms).Generate();
        }
    }
}