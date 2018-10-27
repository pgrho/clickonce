using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Shipwreck.ClickOnce.Manifest
{
    public class ApplicationManifestGenerator
    {
        private const string XSI = "http://www.w3.org/2001/XMLSchema-instance";
        private const string ASM_V1 = "urn:schemas-microsoft-com:asm.v1";
        private const string ASM_V2 = "urn:schemas-microsoft-com:asm.v2";

        private static readonly Regex _EntryPointPattern
            = new Regex(@"^[^/]+\.exe$", RegexOptions.IgnoreCase);

        public ApplicationManifestGenerator(ApplicationManifestSettings settings)
        {
            Settings = settings;
        }

        protected ApplicationManifestSettings Settings { get; }

        #region Input Properties

        #region FromDirectory

        private DirectoryInfo _FromDirectory;

        protected DirectoryInfo FromDirectory
            => _FromDirectory ?? (_FromDirectory = new DirectoryInfo(Settings.FromDirectory?.Length > 0 ? Settings.FromDirectory : Environment.CurrentDirectory));

        #endregion FromDirectory

        #region FromDirectoryUri

        private Uri _FromDirectoryUri;

        protected Uri FromDirectoryUri
            => _FromDirectoryUri ?? (_FromDirectoryUri = new Uri(FromDirectory.FullName + '\\'));

        #endregion FromDirectoryUri

        #region IncludedFilePaths

        private List<string> _IncludedFilePaths;
        protected List<string> IncludedFilePaths => _IncludedFilePaths ?? (_IncludedFilePaths = GetIncludedFilePaths());

        private List<string> GetIncludedFilePaths()
        {
            var include = Minimatch.Compile(Settings.Include);
            var exclude = Minimatch.Compile(Settings.Exclude);

            var paths = new List<string>();

            foreach (var f in FromDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var fu = new Uri(f.FullName);
                var path = Uri.UnescapeDataString(FromDirectoryUri.MakeRelativeUri(fu).ToString());

                if (include(path) && !exclude(path))
                {
                    paths.Add(path);
                }
            }

            return paths;
        }

        #endregion IncludedFilePaths

        #region EntryPointPath

        private string _EntryPointPath;

        protected string EntryPointPath
            => _EntryPointPath ?? (_EntryPointPath = GetEntryPointPath());

        private string GetEntryPointPath()
        {
            var entryPoint = Settings.EntryPoint;
            if (entryPoint == null)
            {
                foreach (var p in IncludedFilePaths)
                {
                    if (_EntryPointPattern.IsMatch(p))
                    {
                        if (IncludedFilePaths.Contains(
                            Path.ChangeExtension(p, ".exe.manifest"),
                            StringComparer.InvariantCultureIgnoreCase))
                        {
                            entryPoint = p;
                            break;
                        }
                    }
                }

                if (entryPoint == null)
                {
                    foreach (var p in IncludedFilePaths)
                    {
                        if (_EntryPointPattern.IsMatch(p))
                        {
                            entryPoint = p;
                            break;
                        }
                    }
                }
            }

            return entryPoint;
        }

        #endregion EntryPointPath

        #region ManifestPath

        private string _ManifestPath;

        protected string ManifestPath
            => _ManifestPath ?? (_ManifestPath = EntryPointPath == null ? null : EntryPointPath + ".manifest");

        #endregion ManifestPath

        #endregion Input Properties

        #region Output Properties

        #region Document

        private XDocument _Document;

        protected XDocument Document => _Document ?? (_Document = CopyOrCreateManifestDocument());

        private XDocument CopyOrCreateManifestDocument()
        {
            XDocument xd;
            var manPath = new Uri(FromDirectoryUri, ManifestPath).LocalPath;
            if (File.Exists(manPath))
            {
                xd = XDocument.Load(manPath);
                var rems = xd.Root.Elements()
                    .Where(e => e.Name == XName.Get("dependency", ASM_V2)
                                || e.Name == XName.Get("file", ASM_V2)).ToList();

                foreach (var e in rems)
                {
                    e.Remove();
                }
            }
            //else
            {
                xd = new XDocument();
                var root = new XElement(
                    XName.Get("assembly", ASM_V1),
                    new XAttribute("xmlns", ASM_V2),
                    new XAttribute(XNamespace.Xmlns + "asmv1", ASM_V1));

                root.SetAttributeValue(XName.Get("schemaLocation", XSI), "urn:schemas-microsoft-com:asm.v1 assembly.adaptive.xsd");
                root.SetAttributeValue("manifestVersion", "1.0");

                xd.Add(root);
            }

            return xd;
        }

        #endregion Document

        #region ToDirectory

        private DirectoryInfo _ToDirectory;

        protected DirectoryInfo ToDirectory
            => _ToDirectory ?? (_ToDirectory = new DirectoryInfo(Settings.ToDirectory?.Length > 0 ? Path.GetFullPath(Settings.ToDirectory) : FromDirectory.FullName));

        #endregion ToDirectory

        #region ToDirectoryUri

        private Uri _ToDirectoryUri;
        protected Uri ToDirectoryUri => _ToDirectoryUri ?? (_ToDirectoryUri = new Uri(ToDirectory.FullName + "\\"));

        #endregion ToDirectoryUri

        #endregion Output Properties

        public void Generate()
        {
            GenerateMetadataElements();

            GeneratePathElements();

            CopyFiles();

            SaveDocument();
        }

        #region GenerateMetadataElements

        private void GenerateMetadataElements()
        {
            GenerateAssemblyIdentityElement();
            GenerateDescriptionElement();
            GenerateApplicationElement();
            GenerateEntryPointElement();

            // TODO: trustInfo
            // TODO: dependency/dependentOS

            GenerateFrameworkDependencies();
        }

        private void GenerateAssemblyIdentityElement()
        {
            if (EntryPointPath != null)
            {
                var rootAsmElem = new XElement(XName.Get("assemblyIdentity", ASM_V1));
                rootAsmElem.SetAttributeValue("name", EntryPointPath.Replace('/', '\\'));
                // TODO: application version
                rootAsmElem.SetAttributeValue("type", "win32");

                var fi = new FileInfo(Path.Combine(FromDirectory.FullName, EntryPointPath));
                var asm = Assembly.ReflectionOnlyLoadFrom(fi.FullName);
                var name = asm.GetName();

                rootAsmElem.SetAttributeValue("version", Settings.Version?.ToString() ?? "1.0.0.0");
                SetAssemblyAttributes(rootAsmElem, name, true);

                Document.Root.Add(rootAsmElem);
            }
        }

        private void GenerateApplicationElement()
        {
            Document.Root.Add(new XElement(XName.Get("application", ASM_V2)));
        }

        private void GenerateDescriptionElement()
        {
            // TODO: v1:description @v2:iconFile
            Document.Root.Add(new XElement(XName.Get("description", ASM_V1)));
        }

        private void GenerateEntryPointElement()
        {
            if (EntryPointPath != null)
            {
                var epe = new XElement(XName.Get("entryPoint", ASM_V2));
                Document.Root.Add(epe);

                var ai = new XElement(XName.Get("assemblyIdentity", ASM_V2));
                var fi = new FileInfo(Path.Combine(FromDirectory.FullName, EntryPointPath));
                var asm = Assembly.ReflectionOnlyLoadFrom(fi.FullName);
                var name = asm.GetName();
                ai.SetAttributeValue("name", name.Name);
                ai.SetAttributeValue("version", name.Version.ToString());
                SetAssemblyAttributes(ai, name);
                epe.Add(ai);

                var cl = new XElement(XName.Get("commandLine", ASM_V2));
                cl.SetAttributeValue("file", EntryPointPath);
                // TODO: parameters
                cl.SetAttributeValue("parameters", "");
                epe.Add(cl);
            }
        }

        private void GenerateFrameworkDependencies()
        {
            var dep = new XElement(XName.Get("dependency", ASM_V2));
            Document.Root.Add(dep);

            var da = new XElement(XName.Get("dependentAssembly", ASM_V2));
            da.SetAttributeValue("dependencyType", "preRequisite");
            da.SetAttributeValue("allowDelayedBinding", "true");
            dep.Add(da);

            var ai = new XElement(XName.Get("assemblyIdentity", ASM_V2));
            ai.SetAttributeValue("name", "Microsoft.Windows.CommonLanguageRuntime");
            // TODO: determine assembly version
            ai.SetAttributeValue("version", "4.0.30319.0");
            da.Add(ai);
        }

        #endregion GenerateMetadataElements

        #region GeneratePathElements

        private void GeneratePathElements()
        {
            List<XElement> files = null;
            foreach (var p in IncludedFilePaths)
            {
                if (ManifestPath?.Equals(p, StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    continue;
                }

                var fi = new FileInfo(new Uri(FromDirectoryUri, p).LocalPath);

                if (p.EndsWith(".exe") || p.EndsWith(".dll"))
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

        private static XElement CreateDependencyElement(FileInfo file, string path)
        {
            var asm = Assembly.ReflectionOnlyLoadFrom(file.FullName);
            var name = asm.GetName();

            var dep = new XElement(XName.Get("dependency", ASM_V2));

            var da = new XElement(XName.Get("dependentAssembly", ASM_V2));
            da.SetAttributeValue("dependencyType", "install");
            da.SetAttributeValue("allowDelayedBinding", "true");
            da.SetAttributeValue("codebase", path.Replace('/', '\\'));
            da.SetAttributeValue("size", file.Length);

            var ai = new XElement(XName.Get("assemblyIdentity", ASM_V2));
            ai.SetAttributeValue("name", name.Name);
            ai.SetAttributeValue("version", name.Version);

            SetAssemblyAttributes(ai, name);

            dep.Add(da);
            da.Add(ai);
            return dep;
        }

        private static XElement CreateFileElement(string p, FileInfo fi)
        {
            var fe = new XElement(XName.Get("file", ASM_V2));
            fe.SetAttributeValue("name", p.Replace('/', '\\'));
            fe.SetAttributeValue("size", fi.Length);
            return fe;
        }

        private static void SetAssemblyAttributes(XElement ai, AssemblyName name, bool emptyKeyToken = false)
        {
            ai.SetAttributeValue("language", name.CultureName?.Length > 0 ? name.CultureName : "neutral");
            var keyToken = name.GetPublicKeyToken();
            if (keyToken?.Length == 8)
            {
                ai.SetAttributeValue("publicKeyToken", string.Concat(keyToken.Select(b => b.ToString("X2"))));
            }
            else if (emptyKeyToken)
            {
                ai.SetAttributeValue("publicKeyToken", "0000000000000000");
            }
            ai.SetAttributeValue(
                "processorArchitecture",
                name.ProcessorArchitecture == ProcessorArchitecture.MSIL ? "msil"
                : name.ProcessorArchitecture == ProcessorArchitecture.X86 ? "x86"
                : name.ProcessorArchitecture == ProcessorArchitecture.Amd64 ? "amd64"
                : null);
        }

        #endregion GeneratePathElements

        private void CopyFiles()
        {
            if (!FromDirectory.FullName.Equals(ToDirectory.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var p in IncludedFilePaths)
                {
                    var dest = new FileInfo(new Uri(ToDirectoryUri, p).LocalPath);
                    if (!dest.Directory.Exists)
                    {
                        dest.Directory.Create();
                    }

                    File.Copy(new Uri(FromDirectoryUri, p).LocalPath, dest.FullName, true);
                }
            }
        }

        private void SaveDocument()
        {
            Document.Save(new Uri(ToDirectoryUri, ManifestPath).LocalPath);
        }
    }
}