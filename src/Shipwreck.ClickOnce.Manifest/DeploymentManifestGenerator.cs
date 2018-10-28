using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Shipwreck.ClickOnce.Manifest
{
    public class DeploymentManifestGenerator : ManifestGenerator
    {
        protected static readonly XNamespace ClickOnceV1 = "urn:schemas-microsoft-com:clickonce.v1";

        private static readonly Regex _ManifestPattern
            = new Regex(@"^[^/]+\.manifest$", RegexOptions.IgnoreCase);

        public DeploymentManifestGenerator(DeploymentManifestSettings settings)
            : base(settings)
        { }

        protected new DeploymentManifestSettings Settings
            => (DeploymentManifestSettings)base.Settings;

        #region Input Properties

        #region ApplicationManifest

        private XDocument _ApplictionManifest;

        protected XDocument ApplicationManifest
            => _ApplictionManifest
            ?? (_ApplictionManifest = ManifestPath == null ? null : XDocument.Load(Path.Combine(FromDirectory.FullName, ManifestPath)));

        #endregion ApplicationManifest

        #region ApplicationName

        private string _ApplicationName;

        protected string ApplicationName
            => _ApplicationName
            ?? (_ApplicationName = Settings.ApplicationName
            ?? (ManifestPath == null ? null
            : Path.GetFileNameWithoutExtension(ApplicationManifest?.Root?.Element(AsmV2 + "assemblyIdentity")?.Attribute("name")?.Value ?? Path.GetFileNameWithoutExtension(ManifestPath))));

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

        public void Generate()
        {
            GenerateMetadataElements();

            GeneratePathElements();

            SaveDocument();
        }

        protected void GenerateMetadataElements()
        {
            GenerateAssemblyIdentityElement();
            GenerateDescriptionElement();
            GenerateDeploymentElement();

            // TODO: deployment
            // TODO: compatibleFrameworks
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
            e.SetAttributeValue(ClickOnceV1 + "createDesktopShortcut", Settings.CreateDesktopShortcut.ToAttributeValue());

            var f = Settings.CodeBaseFolder;
            if (f?.Length > 0)
            {
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

        protected override void GeneratePathElements()
        {
            var da = Document.Root.GetOrAdd(AsmV2 + "dependency")
                    .GetOrAdd(AsmV2 + "dependentAssembly");

            var mp = Uri.UnescapeDataString(ToDirectoryUri.MakeRelativeUri(new Uri(FromDirectoryUri, ManifestPath)).ToString()).Replace('/', '\\');

            da.SetAttributeValue("dependencyType", "install");
            da.SetAttributeValue("codebase", mp);
            da.SetAttributeValue("size", new FileInfo(new Uri(FromDirectoryUri, ManifestPath).LocalPath).Length);

            var sai = Document.Root.Element(AsmV1 + "assemblyIdentity");

            var ai = da.GetOrAdd(AsmV2 + "assemblyIdentity");
            ai.SetAttributeValue("name", Path.GetFileNameWithoutExtension(ManifestPath));

            ai.SetAttributeValue("version", sai?.Attribute("version")?.Value);
            ai.SetAttributeValue("publicKeyToken", sai?.Attribute("publicKeyToken")?.Value);
            ai.SetAttributeValue("language", sai?.Attribute("language")?.Value);
            ai.SetAttributeValue("processorArchitecture", sai?.Attribute("processorArchitecture")?.Value);

            ai.SetAttributeValue("type", "win32");
        }

        protected override void SaveDocument()
        {
            var p = new Uri(ToDirectoryUri, ApplicationName + ".application").LocalPath;
            TraceSource.TraceInformation("Writing Manifest to {0}", p);
            TraceSource.TraceEvent(TraceEventType.Verbose, 0, "Manifest Content: {0}", Document);
            Document.Save(p);
        }
    }
}