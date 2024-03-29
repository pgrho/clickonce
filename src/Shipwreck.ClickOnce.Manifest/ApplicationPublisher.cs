﻿using System.Diagnostics;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest;

[DataContract]
public class ApplicationPublisher
{
    protected internal static readonly TraceSource TraceSource
        = ManifestGenerator.TraceSource;

    private Action<string> _Log;

    public ApplicationPublisher(PublishSettings settings)
    {
        Settings = settings;
    }

    protected PublishSettings Settings { get; }

    public void Generate(Action<string> log = null)
    {
        _Log = log;
        var td = new DirectoryInfo(Settings.ToDirectory);

        if (Settings.DeleteDirectory)
        {
            if (td.Exists)
            {
                TraceInformation("Removing Directory :{0}", td.FullName);
                td.Delete(true);
            }
        }

        var ams = new ApplicationManifestSettings()
        {
            EntryPoint = Settings.EntryPoint,
            IconFile = Settings.IconFile,
            GeneratesLauncher = Settings.GeneratesLauncher,
            MapFileExtensions = Settings.MapFileExtensions,
            Version = Settings.Version,
            PermissionSet = Settings.PermissionSet,
            SameSite = Settings.SameSite,
            CustomPermissionSet = Settings.CustomPermissionSet,

            FromDirectory = Settings.FromDirectory,
            ToDirectory = Settings.ToDirectory?.Length > 0 ? Path.Combine(Settings.ToDirectory, "__application.publish") : "__application.publish",

            Include = Settings.Include,
            Exclude = Settings.Exclude,
            DependentAssemblies = Settings.DependentAssemblies,

            Overwrite = Settings.Overwrite,
            DeleteDirectory = Settings.DeleteDirectory,

            IncludeHash = Settings.IncludeHash,

            Certificate = Settings.Certificate,
            CertificateThumbprint = Settings.CertificateThumbprint,
            CertificateFileName = Settings.CertificateFileName,
            CertificateRawData = Settings.CertificateRawData,
            CertificatePassword = Settings.CertificatePassword,
            CertificateSecurePassword = Settings.CertificateSecurePassword,
            TimestampUrl = Settings.TimestampUrl,
            MaxPasswordRetryCount = Settings.MaxPasswordRetryCount,

            FileAssociations = Settings.FileAssociations,
        };

        var ag = new ApplicationManifestGenerator(ams);

        ag.Generate(log: log);

        var vs = Settings.Version?.ToString()
                 ?? ag.Document.Root.Element(ManifestGenerator.AsmV1 + "assemblyIdentity")?.Attribute("version")?.Value
                 ?? "1.0.0.0";

        var af = $"Application Files/{Path.GetFileNameWithoutExtension(ag.EntryPointPath)}_{vs.Replace('.', '_')}";
        var nd = Settings.ToDirectory?.Length > 0 ? Path.Combine(Settings.ToDirectory, af) : af;

        if (nd != ams.ToDirectory)
        {
            var ndi = new DirectoryInfo(nd);
            if (ndi?.Parent.Exists == false)
            {
                ndi.Parent.Create();
            }

            Directory.Move(ams.ToDirectory, nd);
        }

        var dms = new DeploymentManifestSettings()
        {
            ApplicationName = Settings.ApplicationName,
            Version = Version.Parse(vs),
            MapFileExtensions = Settings.MapFileExtensions,

            CompatibleFrameworks = Settings.CompatibleFrameworks?.Select(e => e.Clone()).ToList(),

            Publisher = Settings.Publisher,
            SuiteName = Settings.SuiteName,
            Product = Settings.Product,
            SupportUrl = Settings.SupportUrl,
            ErrorReportUrl = Settings.ErrorReportUrl,

            Install = Settings.Install,
            CreateDesktopShortcut = Settings.CreateDesktopShortcut,
            MinimumRequiredVersion = Settings.MinimumRequiredVersion,
            UpdateAfterStartup = Settings.UpdateAfterStartup,
            MaximumAge = Settings.MaximumAge,
            MaximumAgeUnit = Settings.MaximumAgeUnit,
            UpdateBeforeStartup = Settings.UpdateBeforeStartup,
            CodeBaseFolder = Settings.CodeBaseFolder,

            FromDirectory = nd,
            ToDirectory = Settings.ToDirectory,

            Overwrite = Settings.Overwrite,

            IncludeHash = Settings.IncludeHash,

            Certificate = Settings.Certificate,
            CertificateThumbprint = Settings.CertificateThumbprint,
            CertificateFileName = Settings.CertificateFileName,
            CertificateRawData = Settings.CertificateRawData,
            CertificatePassword = Settings.CertificatePassword,
            CertificateSecurePassword = Settings.CertificateSecurePassword,
            TimestampUrl = Settings.TimestampUrl,
            MaxPasswordRetryCount = Settings.MaxPasswordRetryCount,
        };

        new DeploymentManifestGenerator(dms).Generate(log: log);
    }

    protected void TraceInformation(string message) => TraceEvent(TraceEventType.Information, message);
    protected void TraceInformation(string format, params object[] args) => TraceEvent(TraceEventType.Information, format, args);
    protected void TraceError(string message) => TraceEvent(TraceEventType.Error, message);
    protected void TraceError(string format, params object[] args) => TraceEvent(TraceEventType.Error, format, args);

    protected virtual void TraceEvent(TraceEventType eventType, string message)
    {
        TraceSource?.TraceEvent(eventType, 0, message);
        _Log?.Invoke(typeof(ApplicationPublisher).FullName + "[" + eventType + "]: " + message);
    }

    protected virtual void TraceEvent(TraceEventType eventType, string format, params object[] args)
    {
        TraceSource?.TraceEvent(eventType, 0, format, args);
        _Log?.Invoke(typeof(ApplicationPublisher).FullName + "[" + eventType + "]: " + string.Format(format, args));
    }
}