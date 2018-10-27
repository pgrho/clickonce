using System;
using System.Diagnostics;
using System.IO;
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
                    version: Settings.Version?.ToString() ?? "1.0.0.0",
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

        protected override void SaveDocument()
        {
            var p = new Uri(ToDirectoryUri, ApplicationName + ".application").LocalPath;
            TraceSource.TraceInformation("Writing Manifest to {0}", p);
            TraceSource.TraceEvent(TraceEventType.Verbose, 0, "Manifest Content: {0}", Document);
            Document.Save(p);
        }
    }
}