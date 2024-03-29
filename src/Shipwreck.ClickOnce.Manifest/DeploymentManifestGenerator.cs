﻿using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Shipwreck.ClickOnce.Manifest;

public class DeploymentManifestGenerator : ManifestGenerator
{
    private static readonly Regex _ManifestPattern
        = new(@"^[^/]+\.manifest$", RegexOptions.IgnoreCase);

    private Action<string> _Log;

    public DeploymentManifestGenerator(DeploymentManifestSettings settings)
        : base(settings)
    { }

    protected new DeploymentManifestSettings Settings
        => (DeploymentManifestSettings)base.Settings;

    #region Input Properties

    #region ApplicationManifest

    private XDocument _ApplictionManifest;

    public XDocument ApplicationManifest
        => _ApplictionManifest ??= ManifestPath == null ? null : XDocument.Load(Path.Combine(FromDirectory.FullName, ManifestPath));

    #endregion ApplicationManifest

    #region ApplicationName

    private string _ApplicationName;

    public string ApplicationName
        => _ApplicationName ??= Settings.ApplicationName
        ?? (ManifestPath == null ? null
        : Path.GetFileNameWithoutExtension(ApplicationManifest?.Root?.Element(AsmV2 + "assemblyIdentity")?.Attribute("name")?.Value ?? Path.GetFileNameWithoutExtension(ManifestPath)));

    #endregion ApplicationName

    protected override string GetManifestPath()
    {
        var am = Settings.ApplicationManifest;
        if (am != null)
        {
            return am;
        }
        var appName = Settings.ApplicationName;

        if (appName != null)
        {
            var fn = appName + ".manifest";
            foreach (var p in IncludedFilePaths)
            {
                if (fn.Equals(Path.GetFileName(p), StringComparison.InvariantCultureIgnoreCase))
                {
                    return p;
                }
            }
        }

        foreach (var p in IncludedFilePaths)
        {
            if (_ManifestPattern.IsMatch(p))
            {
                return p;
            }
        }
        return null;
    }

    #endregion Input Properties

    protected override XDocument CopyOrCreateManifestDocument()
    {
        var d = base.CopyOrCreateManifestDocument();
        d.Root.SetAttributeValue(XNamespace.Xmlns + "asmv2", AsmV2.NamespaceName);
        d.Root.SetAttributeValue(XNamespace.Xmlns + "co.v1", ClickOnceV1.NamespaceName);
        return d;
    }

    protected override string GetOutputFileName()
        => new Uri(ToDirectoryUri, ApplicationName + ".application").LocalPath;

    public void Generate(Action<string> log = null)
    {
        _Log = log;

        GenerateMetadataElements();

        GeneratePathElements();

        SaveDocument();
    }

    protected void GenerateMetadataElements()
    {
        GenerateAssemblyIdentityElement();
        GenerateDescriptionElement();
        GenerateDeploymentElement();
        GenerateCompatibleFrameworksElement();
    }

    protected void GenerateAssemblyIdentityElement()
    {
        if (ApplicationManifest != null)
        {
            var ai = ApplicationManifest.Root.Element(AsmV1 + "assemblyIdentity");

            GetOrAddAssemblyIdentityElement(
                name: ApplicationName + ".application",
                version: Settings.Version?.ToString() ?? ai?.Attribute("version")?.Value ?? "1.0.0.0",
                language: ai?.Attribute("language")?.Value,
                processorArchitecture: ai?.Attribute("processorArchitecture")?.Value,
                publicKeyToken: ai?.Attribute("publicKeyToken")?.Value);
        }
    }

    protected void GenerateDescriptionElement()
    {
        var e = Document.Root.GetOrAdd(AsmV1 + "description");

        e.SetAttributeValue(AsmV2 + "publisher", Settings.Publisher);
        e.SetAttributeValue(ClickOnceV1 + "suiteName", Settings.SuiteName);
        e.SetAttributeValue(AsmV2 + "product", Settings.Product ?? ApplicationName);
        e.SetAttributeValue(AsmV2 + "supportUrl", Settings.SupportUrl);
        e.SetAttributeValue(ClickOnceV1 + "errorReportUrl", Settings.ErrorReportUrl);
    }

    protected void GenerateDeploymentElement()
    {
        var e = Document.Root.GetOrAdd(AsmV2 + "deployment");

        e.SetAttributeValue("install", Settings.Install.ToAttributeValue());
        e.SetAttributeValue("mapFileExtensions", Settings.MapFileExtensions ? "true" : null);
        e.SetAttributeValue(ClickOnceV1 + "createDesktopShortcut", Settings.CreateDesktopShortcut.ToAttributeValue());
        e.SetAttributeValue("minimumRequiredVersion", Settings.MinimumRequiredVersion?.ToString());

        var f = Settings.CodeBaseFolder;
        if (f?.Length > 0)
        {
            if (Settings.UpdateAfterStartup)
            {
                var exp = e.AddElement(AsmV2 + "subscription")
                        .AddElement(AsmV2 + "update")
                        .AddElement(AsmV2 + "expiration");

                exp.SetAttributeValue("maximumAge", Settings.MaximumAge);
                exp.SetAttributeValue("unit", Settings.MaximumAgeUnit.ToString("G").ToLowerInvariant());
            }
            if (Settings.UpdateBeforeStartup)
            {
                e.AddElement(AsmV2 + "subscription")
                    .AddElement(AsmV2 + "update")
                    .AddElement(AsmV2 + "beforeApplicationStartup");
            }

            e.GetOrAdd(AsmV2 + "deploymentProvider")
                .SetAttributeValue(
                    "codebase",
                    f
                    + (f.Last() == '/'
                       || f.Last() == '\\' ? null
                        : f.StartsWith("http:")
                          || f.StartsWith("https:")
                          || f.StartsWith("ftp:")
                          || f.StartsWith("ftps:") ? "/" : "\\")
                    + ApplicationName
                    + ".application");
        }
    }

    protected void GenerateCompatibleFrameworksElement()
    {
        IEnumerable<CompatibleFramework> cfs = Settings.CompatibleFrameworks;
        if (!cfs.Any())
        {
            var mp = new Uri(FromDirectoryUri, ManifestPath).LocalPath;
            var cp = Path.Combine(Path.GetDirectoryName(mp), Path.ChangeExtension(mp, Settings.MapFileExtensions ? ".config.deploy" : ".config"));

            if (File.Exists(cp))
            {
                var cd = XDocument.Load(cp);

                var sre = cd.Element("configuration")?.Element("startup")?.Element("supportedRuntime");
                if (sre != null)
                {
                    var v = sre.Attribute("version")?.Value;
                    var sku = sre.Attribute("sku")?.Value;

                    if (v?.Length > 0 && sku?.Length > 0)
                    {
                        var sps = sku.Split(',').Select(e => e.Trim());

                        var cf = new CompatibleFramework()
                        {
                            SupportedRuntime = v == "v4.0" ? "4.0.30319" : v.TrimStart('v'),
                            Profile = sps.FirstOrDefault(e => e.StartsWith("Profile="))?.Substring(8) ?? "Full",
                            TargetVersion = sps.FirstOrDefault(e => e.StartsWith("Version="))?.Substring(8).TrimStart('v')
                        };

                        cfs = new[] { cf };
                    }
                }
            }
        }

        if (cfs.Any())
        {
            var pe = Document.Root.GetOrAdd(ClickOnceV2 + "compatibleFrameworks");

            foreach (var cf in cfs)
            {
                var ce = new XElement(ClickOnceV2 + "framework");
                ce.SetAttributeValue("targetVersion", cf.TargetVersion);
                ce.SetAttributeValue("profile", cf.Profile);
                ce.SetAttributeValue("supportedRuntime", cf.SupportedRuntime);

                pe.Add(ce);
            }
        }
    }

    protected override void GeneratePathElements()
    {
        var da = Document.Root.GetOrAdd(AsmV2 + "dependency")
                .GetOrAdd(AsmV2 + "dependentAssembly");

        var mp = Uri.UnescapeDataString(ToDirectoryUri.MakeRelativeUri(new Uri(FromDirectoryUri, ManifestPath)).ToString()).Replace('/', '\\');

        var fi = new FileInfo(new Uri(FromDirectoryUri, ManifestPath).LocalPath);
        da.SetAttributeValue("dependencyType", "install");
        da.SetAttributeValue("codebase", mp);
        da.SetAttributeValue("size", fi.Length);

        var sai = Document.Root.Element(AsmV1 + "assemblyIdentity");

        var ai = da.GetOrAdd(AsmV2 + "assemblyIdentity");
        ai.SetAttributeValue("name", Path.GetFileNameWithoutExtension(ManifestPath));

        ai.SetAttributeValue("version", sai?.Attribute("version")?.Value);
        ai.SetAttributeValue("publicKeyToken", sai?.Attribute("publicKeyToken")?.Value);
        ai.SetAttributeValue("language", sai?.Attribute("language")?.Value);
        ai.SetAttributeValue("processorArchitecture", sai?.Attribute("processorArchitecture")?.Value);
        ai.SetAttributeValue("type", "win32");

        AddHashElement(da, fi);
    }

    protected override void TraceEvent(TraceEventType eventType, string message)
    {
        base.TraceEvent(eventType, message);
        Log?.Invoke(typeof(DeploymentManifestGenerator).FullName + "[" + eventType + "]: " + message);
    }

    protected override void TraceEvent(TraceEventType eventType, string format, params object[] args)
    {
        base.TraceEvent(eventType, format, args);
        Log?.Invoke(typeof(DeploymentManifestGenerator).FullName + "[" + eventType + "]: " + string.Format(format, args));
    }
}