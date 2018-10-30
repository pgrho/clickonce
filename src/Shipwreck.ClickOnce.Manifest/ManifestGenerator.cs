using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Build.Tasks.Deployment.ManifestUtilities;

namespace Shipwreck.ClickOnce.Manifest
{
    public abstract class ManifestGenerator
    {
        protected internal static readonly TraceSource TraceSource
            = new TraceSource(typeof(ApplicationManifestGenerator).Namespace);

        protected internal static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
        protected internal static readonly XNamespace AsmV1 = "urn:schemas-microsoft-com:asm.v1";
        protected internal static readonly XNamespace AsmV2 = "urn:schemas-microsoft-com:asm.v2";
        protected internal static readonly XNamespace AsmV3 = "urn:schemas-microsoft-com:asm.v3";
        protected internal static readonly XNamespace Dsig = "http://www.w3.org/2000/09/xmldsig#";

        protected ManifestGenerator(ManifestSettings settings)
            => Settings = settings;

        protected ManifestSettings Settings { get; }

        #region Input Properties

        #region FromDirectory

        private DirectoryInfo _FromDirectory;

        public DirectoryInfo FromDirectory
            => _FromDirectory ?? (_FromDirectory = new DirectoryInfo(Settings.FromDirectory?.Length > 0 ? Settings.FromDirectory : Environment.CurrentDirectory));

        #endregion FromDirectory

        #region FromDirectoryUri

        private Uri _FromDirectoryUri;

        protected internal Uri FromDirectoryUri
            => _FromDirectoryUri ?? (_FromDirectoryUri = new Uri(FromDirectory.FullName.Trim('/', '\\') + '\\'));

        #endregion FromDirectoryUri

        #region IncludedFilePaths

        private List<string> _IncludedFilePaths;
        public List<string> IncludedFilePaths => _IncludedFilePaths ?? (_IncludedFilePaths = GetIncludedFilePaths());

        private List<string> GetIncludedFilePaths()
        {
            TraceSource.TraceInformation("Searching files from {0}", FromDirectory.FullName);

            var include = Minimatch.Compile(Settings.Include);
            var exclude = Minimatch.Compile(Settings.Exclude);

            var paths = new List<string>();

            foreach (var f in FromDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var fu = new Uri(f.FullName);
                var path = Uri.UnescapeDataString(FromDirectoryUri.MakeRelativeUri(fu).ToString());

                if (include(path) && !exclude(path))
                {
                    TraceSource.TraceEvent(TraceEventType.Verbose, 0, "Found: {0}", path);

                    paths.Add(path);
                }
            }

            return paths;
        }

        #endregion IncludedFilePaths

        #region ManifestPath

        private string _ManifestPath;

        public string ManifestPath
            => _ManifestPath ?? (_ManifestPath = GetManifestPath());

        protected abstract string GetManifestPath();

        #endregion ManifestPath

        #endregion Input Properties

        #region Output Properties

        #region Document

        private XDocument _Document;

        public XDocument Document => _Document ?? (_Document = CopyOrCreateManifestDocument());

        protected virtual XDocument CopyOrCreateManifestDocument()
        {
            var xd = new XDocument();
            var root = new XElement(
                AsmV1 + "assembly",
                new XAttribute("xmlns", AsmV2.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "asmv1", AsmV1.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "dsig", Dsig.NamespaceName));

            root.SetAttributeValue(Xsi + "schemaLocation", "urn:schemas-microsoft-com:asm.v1 assembly.adaptive.xsd");
            root.SetAttributeValue("manifestVersion", "1.0");

            xd.Add(root);

            return xd;
        }

        #endregion Document

        #region ToDirectory

        private DirectoryInfo _ToDirectory;

        public DirectoryInfo ToDirectory
            => _ToDirectory ?? (_ToDirectory = new DirectoryInfo(Settings.ToDirectory?.Length > 0 ? Path.GetFullPath(Settings.ToDirectory) : FromDirectory.FullName));

        #endregion ToDirectory

        #region ToDirectoryUri

        private Uri _ToDirectoryUri;
        protected internal Uri ToDirectoryUri => _ToDirectoryUri ?? (_ToDirectoryUri = new Uri(ToDirectory.FullName.Trim('/', '\\') + "\\"));

        #endregion ToDirectoryUri

        private string _OutputFileName;

        protected string OutputFileName
            => _OutputFileName ?? (_OutputFileName = GetOutputFileName());

        protected abstract string GetOutputFileName();

        #endregion Output Properties

        protected XElement GetOrAddAssemblyIdentityElement(
            string name = null,
            string version = null,
            string language = null,
            string processorArchitecture = null,
            string publicKeyToken = null,
            string type = null)
        {
            var e = Document.Root.GetOrAdd(AsmV1 + "assemblyIdentity");

            e.SetAttributeValue("name", name);
            e.SetAttributeValue("version", version);
            e.SetAttributeValue("language", language);
            e.SetAttributeValue("processorArchitecture", processorArchitecture);
            e.SetAttributeValue("publicKeyToken", publicKeyToken);
            e.SetAttributeValue("type", type);

            return e;
        }

        #region GeneratePathElements

        protected abstract void GeneratePathElements();

        protected void AddPathElements(IEnumerable<string> paths)
        {
            List<XElement> files = null;
            var dep = Minimatch.Compile(Settings.DependentAssemblies);
            foreach (var p in paths)
            {
                var fi = new FileInfo(new Uri(FromDirectoryUri, p).LocalPath);

                if (dep(p))
                {
                    try
                    {
                        Document.Root.Add(CreateDependencyElement(fi, p));
                        continue;
                    }
                    catch
                    {
                    }
                }

                (files ?? (files = new List<XElement>())).Add(CreateFileElement(p, fi));
            }

            if (files != null)
            {
                foreach (var f in files)
                {
                    Document.Root.Add(f);
                }
            }
        }

        protected virtual XElement CreateDependencyElement(FileInfo file, string path)
        {
            var name = AssemblyName.GetAssemblyName(file.FullName);

            var dep = new XElement(AsmV2 + "dependency");

            var da = dep.AddElement(AsmV2 + "dependentAssembly");
            da.SetAttributeValue("dependencyType", "install");
            da.SetAttributeValue("allowDelayedBinding", "true");
            da.SetAttributeValue("codebase", path.Replace('/', '\\'));
            da.SetAttributeValue("size", file.Length);

            var ai = da.AddElement(AsmV2 + "assemblyIdentity");
            ai.SetAttributeValue("name", name.Name);
            ai.SetAttributeValue("version", name.Version);

            SetAssemblyAttributes(ai, name);

            AddHashElement(da, file);

            return dep;
        }

        protected virtual XElement CreateFileElement(string p, FileInfo fi)
        {
            var fe = new XElement(AsmV2 + "file");
            fe.SetAttributeValue("name", p.Replace('/', '\\'));
            fe.SetAttributeValue("size", fi.Length);
            AddHashElement(fe, fi);
            return fe;
        }

        protected static void SetAssemblyAttributes(XElement ai, AssemblyName name)
        {
            ai.SetAttributeValue("language", name.CultureName?.Length > 0 ? name.CultureName : "neutral");
            ai.SetAttributeValue("publicKeyToken", name.GetPublicKeyToken().ToAttributeValue());
            ai.SetAttributeValue(
                "processorArchitecture",
                name.ProcessorArchitecture.ToAttributeValue());
        }

        protected void AddHashElement(XElement e, FileInfo f)
        {
            if (Settings.IncludeHash)
            {
                var h = e.AddElement(AsmV2 + "hash");
                h.AddElement(Dsig + "DigestMethod").SetAttributeValue("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha256");
                using (var sha = SHA256.Create())
                {
                    h.AddElement(Dsig + "DigestValue").Value = Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(f.FullName)));
                }
            }
        }

        #endregion GeneratePathElements

        protected virtual void CopyFiles()
        {
            if (!FromDirectory.FullName.Equals(ToDirectory.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                if (Settings.DeleteDirectory && ToDirectory.Exists)
                {
                    TraceSource.TraceInformation("Removing Directory :{0}", ToDirectory.FullName);
                    ToDirectory.Delete(true);
                }

                foreach (var p in IncludedFilePaths)
                {
                    var dest = new FileInfo(new Uri(ToDirectoryUri, p + (Settings.MapFileExtensions ? ".deploy" : null)).LocalPath);
                    if (!dest.Directory.Exists)
                    {
                        TraceSource.TraceInformation("Creating Directory :{0}", dest.Directory.FullName);
                        dest.Directory.Create();
                    }

                    TraceSource.TraceInformation("Copying file :{0}", p);
                    File.Copy(new Uri(FromDirectoryUri, p).LocalPath, dest.FullName, Settings.Overwrite);
                }
            }
        }

        protected void SaveDocument()
        {
            var p = OutputFileName;
            var d = new DirectoryInfo(Path.GetDirectoryName(p));

            if (!d.Exists)
            {
                TraceSource.TraceInformation("Creating Directory :{0}", d.FullName);
                d.Create();
            }
            TraceSource.TraceInformation("Writing Manifest to {0}", p);
            TraceSource.TraceEvent(TraceEventType.Verbose, 0, "Manifest Content: {0}", Document);
            Document.Save(p);

            var tu = Settings.TimestampUrl?.Length > 0 ? new Uri(Settings.TimestampUrl) : null;
            if (Settings.CertificateThumbprint?.Length > 0)
            {
                SecurityUtilities.SignFile(Settings.CertificateThumbprint, tu, p);
            }
            else if (Settings.CertificateFileName?.Length > 0)
            {
                var cert = new X509Certificate2(Settings.CertificateFileName, Settings.CertificatePassword, X509KeyStorageFlags.PersistKeySet);
                SecurityUtilities.SignFile(cert, tu, p);
            }
        }

        private static readonly Regex _IconPattern
            = new Regex(@"^[^/]+\.ico$", RegexOptions.IgnoreCase);

        protected static bool IsIco(string p)
            => _IconPattern.IsMatch(p);
    }
}