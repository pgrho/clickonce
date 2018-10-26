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
                var root = new XElement(XName.Get("assembly", ASM_V1));

                root.SetAttributeValue(XName.Get("schemaLocation", XSI), "urn:schemas-microsoft-com:asm.v1 assembly.adaptive.xsd");
                root.SetAttributeValue("manifestVersion", "1.0");

                xd.Add(root);
            }

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
                    ai.SetAttributeValue("language", name.CultureName ?? "neutral");
                    var keyToken = name.GetPublicKeyToken();
                    if (keyToken != null)
                    {
                        ai.SetAttributeValue("publicKeyToken", string.Concat(keyToken.Select(b => b.ToString("X2"))));
                    }
                    ai.SetAttributeValue(
                        "processorArchitecture",
                        name.ProcessorArchitecture == ProcessorArchitecture.MSIL ? "msil"
                        : name.ProcessorArchitecture == ProcessorArchitecture.X86 ? "x86"
                        : name.ProcessorArchitecture == ProcessorArchitecture.Amd64 ? "amd64"
                        : null);
                    da.Add(ai);
                }
                else
                {
                    var fe = new XElement(XName.Get("file", ASM_V2));
                    fe.SetAttributeValue("name", p.Replace('/', '\\'));
                    fe.SetAttributeValue("size", fi.Length);
                    xd.Root.Add(fe);
                }
            }

            Console.WriteLine(xd);

            Console.WriteLine("Entry Point: {0}", entryPoint);
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