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

        public static void Generate(ApplicationManifestSettings settings)
        {
            var dir = new DirectoryInfo(settings.FromDirectory?.Length > 0 ? settings.FromDirectory : Environment.CurrentDirectory);
            var paths = GetIncludedFilePaths(dir, settings);

            var entryPoint = GetEntryPoint(settings, paths);

            var du = new Uri(dir.FullName + '\\');

            XDocument xd;
            var manifest = entryPoint == null ? null : entryPoint + ".manifest";
            xd = CopyOrCreateManifest(du, manifest);

            AddRootAssemblyIdentity(dir, entryPoint, xd);

            // TODO: v1:description @v2:iconFile
            xd.Root.Add(new XElement(XName.Get("description", ASM_V1)));

            xd.Root.Add(new XElement(XName.Get("application", ASM_V2)));

            AddEntryPoint(dir, entryPoint, xd);

            // TODO: trustInfo
            // TODO: dependency/dependentOS

            {
                var dep = new XElement(XName.Get("dependency", ASM_V2));
                xd.Root.Add(dep);

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

            List<XElement> files = null;
            foreach (var p in paths)
            {
                if (manifest?.Equals(p, StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    continue;
                }

                var fi = new FileInfo(new Uri(du, p).LocalPath);

                if (p.EndsWith(".exe") || p.EndsWith(".dll"))
                {
                    var dep = new XElement(XName.Get("dependency", ASM_V2));
                    xd.Root.Add(dep);

                    var da = new XElement(XName.Get("dependentAssembly", ASM_V2));
                    da.SetAttributeValue("dependencyType", "install");
                    da.SetAttributeValue("allowDelayedBinding", "true");
                    da.SetAttributeValue("codebase", p.Replace('/', '\\'));
                    da.SetAttributeValue("size", fi.Length);
                    dep.Add(da);

                    // TODO: native
                    var asm = Assembly.ReflectionOnlyLoadFrom(fi.FullName);
                    var name = asm.GetName();

                    var ai = new XElement(XName.Get("assemblyIdentity", ASM_V2));
                    ai.SetAttributeValue("name", name.Name);
                    ai.SetAttributeValue("version", name.Version);

                    SetAssemblyAttributes(ai, name);

                    da.Add(ai);
                }
                else
                {
                    var fe = new XElement(XName.Get("file", ASM_V2));
                    fe.SetAttributeValue("name", p.Replace('/', '\\'));
                    fe.SetAttributeValue("size", fi.Length);
                    (files ?? (files = new List<XElement>())).Add(fe);
                }
            }

            if (files != null)
            {
                foreach (var f in files)
                {
                    xd.Root.Add(f);
                }
            }

            var todir = new DirectoryInfo(settings.ToDirectory?.Length > 0 ? Path.GetFullPath(settings.ToDirectory) : dir.FullName);

            var tdu = new Uri(todir.FullName + "\\");

            if (!dir.FullName.Equals(todir.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var p in paths)
                {
                    var dest = new FileInfo(new Uri(tdu, p).LocalPath);
                    if (!dest.Directory.Exists)
                    {
                        dest.Directory.Create();
                    }

                    File.Copy(new Uri(du, p).LocalPath, dest.FullName, true);
                }
            }
            xd.Save(new Uri(tdu, manifest).LocalPath);
        }

        private static void AddEntryPoint(DirectoryInfo dir, string entryPoint, XDocument xd)
        {
            var epe = new XElement(XName.Get("entryPoint", ASM_V2));
            xd.Root.Add(epe);

            var ai = new XElement(XName.Get("assemblyIdentity", ASM_V2));
            var fi = new FileInfo(Path.Combine(dir.FullName, entryPoint));
            var asm = Assembly.ReflectionOnlyLoadFrom(fi.FullName);
            var name = asm.GetName();
            ai.SetAttributeValue("name", name.Name);
            ai.SetAttributeValue("version", name.Version.ToString());
            SetAssemblyAttributes(ai, name);
            epe.Add(ai);

            var cl = new XElement(XName.Get("commandLine", ASM_V2));
            cl.SetAttributeValue("file", entryPoint);
            // TODO: parameters
            cl.SetAttributeValue("parameters", "");
            epe.Add(cl);
        }

        private static XDocument CopyOrCreateManifest(Uri du, string manifest)
        {
            XDocument xd;
            var manPath = new Uri(du, manifest).LocalPath;
            //if (File.Exists(manPath))
            //{
            //    xd = XDocument.Load(manPath);
            //    var rems = xd.Root.Elements()
            //        .Where(e => e.Name == XName.Get("dependency", ASM_V2)
            //                || e.Name == XName.Get("file", ASM_V2)).ToList();

            //    foreach (var e in rems)
            //    {
            //        e.Remove();
            //    }
            //}
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

        private static void AddRootAssemblyIdentity(DirectoryInfo dir, string entryPoint, XDocument xd)
        {
            var rootAsmElem = new XElement(XName.Get("assemblyIdentity", ASM_V1));
            rootAsmElem.SetAttributeValue("name", entryPoint?.Replace('/', '\\'));
            // TODO: application version
            rootAsmElem.SetAttributeValue("version", "1.0.0.0");
            rootAsmElem.SetAttributeValue("type", "win32");

            var fi = new FileInfo(Path.Combine(dir.FullName, entryPoint));
            var asm = Assembly.ReflectionOnlyLoadFrom(fi.FullName);
            var name = asm.GetName();

            SetAssemblyAttributes(rootAsmElem, name, true);

            xd.Root.Add(rootAsmElem);
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

        private static List<string> GetIncludedFilePaths(DirectoryInfo dir, ApplicationManifestSettings settings)
        {
            var du = new Uri(dir.FullName + '\\');

            var include = Minimatch.Compile(settings.Include);
            var exclude = Minimatch.Compile(settings.Exclude);

            var paths = new List<string>();

            foreach (var f in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var fu = new Uri(f.FullName);
                var path = Uri.UnescapeDataString(du.MakeRelativeUri(fu).ToString());

                if (include(path) && !exclude(path))
                {
                    paths.Add(path);
                }
            }

            return paths;
        }

        private static string GetEntryPoint(ApplicationManifestSettings settings, List<string> paths)
        {
            var entryPoint = settings.EntryPoint;
            if (entryPoint == null)
            {
                foreach (var p in paths)
                {
                    if (_EntryPointPattern.IsMatch(p))
                    {
                        if (paths.Contains(
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
                    foreach (var p in paths)
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
    }
}