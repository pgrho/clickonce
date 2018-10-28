using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Shipwreck.ClickOnce.Manifest
{
    public class ApplicationManifestGenerator : ManifestGenerator
    {
        private static readonly Regex _EntryPointPattern
            = new Regex(@"^[^/]+\.exe$", RegexOptions.IgnoreCase);

        public ApplicationManifestGenerator(ApplicationManifestSettings settings)
            : base(settings)
        { }

        protected new ApplicationManifestSettings Settings
            => (ApplicationManifestSettings)base.Settings;

        #region Input Properties

        #region EntryPointPath

        private string _EntryPointPath;

        public string EntryPointPath
            => _EntryPointPath ?? (_EntryPointPath = GetEntryPointPath());

        private string GetEntryPointPath()
        {
            var entryPoint = Settings.EntryPoint;

            if (entryPoint != null)
            {
                return entryPoint;
            }

            foreach (var p in IncludedFilePaths)
            {
                if (_EntryPointPattern.IsMatch(p))
                {
                    if (IncludedFilePaths.Contains(
                        Path.ChangeExtension(p, ".exe.manifest"),
                        StringComparer.InvariantCultureIgnoreCase))
                    {
                        return p;
                    }
                }
            }

            foreach (var p in IncludedFilePaths)
            {
                if (_EntryPointPattern.IsMatch(p))
                {
                    return p;
                }
            }

            return null;
        }

        #endregion EntryPointPath

        #region IconFilePath

        private string _IconFilePath;

        public string IconFilePath
            => _IconFilePath ?? (_IconFilePath = Settings.IconFile ?? GetIconFilePath());

        private string GetIconFilePath()
        {
            var ep = EntryPointPath;
            if (ep != null)
            {
                var ip = Path.ChangeExtension(ep, ".ico");

                if (IncludedFilePaths.Contains(ip, StringComparer.InvariantCultureIgnoreCase))
                {
                    return ip;
                }
            }

            foreach (var p in IncludedFilePaths)
            {
                if (IsIco(p))
                {
                    return p;
                }
            }

            return null;
        }

        #endregion IconFilePath

        protected override string GetManifestPath()
            => EntryPointPath == null ? null : EntryPointPath + ".manifest";

        #endregion Input Properties

        #region Output Properties

        protected override XDocument CopyOrCreateManifestDocument()
        {
            XDocument xd;
            var manPath = new Uri(FromDirectoryUri, ManifestPath).LocalPath;
            if (File.Exists(manPath))
            {
                xd = XDocument.Load(manPath);

                var dependency = AsmV2 + "dependency";
                var file = AsmV2 + "file";
                var trustInfo = AsmV2 + "trustInfo";

                var rems = xd.Root.Elements()
                    .Where(e => e.Name == dependency || e.Name == file || e.Name == trustInfo).ToList();

                foreach (var e in rems)
                {
                    e.Remove();
                }
            }
            else
            {
                xd = base.CopyOrCreateManifestDocument();
            }

            return xd;
        }

        #endregion Output Properties

        public void Generate()
        {
            GenerateMetadataElements();

            GeneratePathElements();

            CopyFiles();

            SaveDocument();
        }

        #region GenerateMetadataElements

        protected void GenerateMetadataElements()
        {
            GenerateAssemblyIdentityElement();
            GenerateDescriptionElement();
            GenerateApplicationElement();
            GenerateEntryPointElement();

            GenerateTrustInfoElement();
            GenerateDependentOsElement();

            GenerateFrameworkDependencies();
        }

        private void GenerateAssemblyIdentityElement()
        {
            if (EntryPointPath != null)
            {
                var fi = new FileInfo(Path.Combine(FromDirectory.FullName, EntryPointPath));
                var asm = Assembly.ReflectionOnlyLoadFrom(fi.FullName);
                var name = asm.GetName();

                GetOrAddAssemblyIdentityElement(
                    name: EntryPointPath.Replace('/', '\\'),
                    version: (Settings.Version ?? name.Version)?.ToString() ?? "1.0.0.0",
                    language: name.CultureName?.Length > 0 ? name.CultureName : "neutral",
                    processorArchitecture: name.ProcessorArchitecture.ToAttributeValue(),
                    publicKeyToken: name.GetPublicKeyToken().ToAttributeValue(true) ?? "0000000000000000",
                    type: "win32");
            }
        }

        private void GenerateApplicationElement()
            => Document.Root.GetOrAdd(AsmV2 + "application");

        private void GenerateDescriptionElement()
            => Document.Root.GetOrAdd(AsmV1 + "description")
                .SetAttributeValue(AsmV2 + "iconFile", IconFilePath);

        private void GenerateEntryPointElement()
        {
            if (EntryPointPath != null)
            {
                var epe = Document.Root.GetOrAdd(AsmV2 + "entryPoint");

                var ai = epe.GetOrAdd(AsmV2 + "assemblyIdentity");
                var fi = new FileInfo(Path.Combine(FromDirectory.FullName, EntryPointPath));
                var asm = Assembly.ReflectionOnlyLoadFrom(fi.FullName);
                var name = asm.GetName();
                ai.SetAttributeValue("name", name.Name);
                ai.SetAttributeValue("version", name.Version.ToString());
                SetAssemblyAttributes(ai, name);

                var cl = epe.GetOrAdd(AsmV2 + "commandLine");
                cl.SetAttributeValue("file", EntryPointPath);
                // TODO: parameters
                cl.SetAttributeValue("parameters", "");
            }
        }

        private void GenerateTrustInfoElement()
        {
            var ti = Document.Root.GetOrAdd(AsmV2 + "trustInfo");

            // As PermissionSet element never accept xmlns or prefix, set xmlns in its ancestor.
            // TODO: Simplify Output XML's prefix
            ti.SetAttributeValue("xmlns", AsmV2.NamespaceName);

            var sec = ti.GetOrAdd(AsmV2 + "security");

            var min = sec.GetOrAdd(AsmV2 + "applicationRequestMinimum");

            var ps = min.GetOrAdd(AsmV2 + "PermissionSet");
            ps.SetAttributeValue("Unrestricted", "true");
            ps.SetAttributeValue("ID", "Custom");
            ps.SetAttributeValue("SameSite", "site");

            var dar = min.GetOrAdd(AsmV2 + "defaultAssemblyRequest");
            dar.SetAttributeValue("permissionSetReference", "Custom");

            var el = sec.GetOrAdd(AsmV3 + "requestedPrivileges")
                .GetOrAdd(AsmV3 + "requestedExecutionLevel");
            el.SetAttributeValue("level", "asInvoker");
            el.SetAttributeValue("uiAccess", "false");
        }

        private void GenerateDependentOsElement()
        {
            var os = Document.Root.GetOrAdd(AsmV2 + "dependency")
                .GetOrAdd(AsmV2 + "dependentOS")
                .GetOrAdd(AsmV2 + "osVersionInfo")
                .GetOrAdd(AsmV2 + "os");

            os.SetAttributeValue("majorVersion", 5);
            os.SetAttributeValue("minorVersion", 1);
            os.SetAttributeValue("buildNumber", 2600);
            os.SetAttributeValue("servicePackMajor", 0);
        }

        private void GenerateFrameworkDependencies()
        {
            var da = Document.Root.AddElement(AsmV2 + "dependency")
                                .AddElement(AsmV2 + "dependentAssembly");
            da.SetAttributeValue("dependencyType", "preRequisite");
            da.SetAttributeValue("allowDelayedBinding", "true");

            var ai = da.AddElement(AsmV2 + "assemblyIdentity");
            ai.SetAttributeValue("name", "Microsoft.Windows.CommonLanguageRuntime");
            // TODO: determine assembly version
            ai.SetAttributeValue("version", "4.0.30319.0");
        }

        #endregion GenerateMetadataElements

        protected override void GeneratePathElements()
            => AddPathElements(
                ManifestPath != null
                    ? IncludedFilePaths.Except(new[] { ManifestPath }, StringComparer.InvariantCultureIgnoreCase)
                    : IncludedFilePaths);

        protected override void SaveDocument()
        {
            var p = new Uri(ToDirectoryUri, ManifestPath).LocalPath;
            TraceSource.TraceInformation("Writing Manifest to {0}", p);
            TraceSource.TraceEvent(TraceEventType.Verbose, 0, "Manifest Content: {0}", Document);
            Document.Save(p);
        }
    }
}