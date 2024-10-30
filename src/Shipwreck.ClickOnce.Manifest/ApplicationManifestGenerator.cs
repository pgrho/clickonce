using System.Diagnostics;
using System.Drawing.Printing;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Shipwreck.ClickOnce.Manifest;

public class ApplicationManifestGenerator : ManifestGenerator
{
    private static readonly Regex _EntryPointPattern
        = new(@"^[^/]+\.exe$", RegexOptions.IgnoreCase);



    public ApplicationManifestGenerator(ApplicationManifestSettings settings)
        : base(settings)
    { }

    protected new ApplicationManifestSettings Settings
        => (ApplicationManifestSettings)base.Settings;

    #region Input Properties

    #region EntryPointPath

    private string _EntryPointPath;

    public string EntryPointPath
        => _EntryPointPath ??= GetEntryPointPath();

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
        => _IconFilePath ??= Settings.IconFile ?? GetIconFilePath();

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

    protected override string GetOutputFileName()
        => new Uri(ToDirectoryUri, ManifestPath).LocalPath;

    #endregion Output Properties

    public void Generate(Action<string> log = null)
    {
        Log = log;

        string launcherPath = null;
        if (Settings.GeneratesLauncher && EntryPointPath != null)
        {
            var lb = new Microsoft.Build.Tasks.Deployment.ManifestUtilities.LauncherBuilder(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft SDKs", "ClickOnce Bootstrapper", "Engine", "Launcher.exe"));
            var msgs = lb.Build(EntryPointPath, FromDirectory.FullName);

            if (!msgs.Succeeded)
            {
                throw new Exception("Failed to build Launcher.exe." + string.Concat(msgs.Messages.Select(e => $"{Environment.NewLine}[{e.Severity}]{e.Message}")));
            }
            launcherPath = Path.Combine(FromDirectory.FullName, "Launcher.exe");
            IncludedFilePaths.Add("Launcher.exe");
        }

        try
        {
            GenerateMetadataElements();

            GeneratePathElements();

            if (Settings.ShouldSerializeFileAssociations())
            {
                foreach (var fa in Settings.FileAssociations)
                {
                    Document.Root.Add(
                        new XElement(ClickOnceV1 + "fileAssociation")
                                .SetAttr("extension", fa.Extension)
                                .SetAttr("description", fa.Description)
                                .SetAttr("progid", fa.ProgId)
                                .SetAttr("defaultIcon", fa.DefaultIcon));
                }
            }

            CopyFiles();

            SaveDocument();
        }
        finally
        {
            if (launcherPath != null)
            {
                try
                {
                    File.Delete(launcherPath);
                }
                catch { }
            }
        }
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
            var name = AssemblyName.GetAssemblyName(fi.FullName);

            GetOrAddAssemblyIdentityElement(
                name: EntryPointPath.Replace('/', '\\'),
                version: (Settings.Version ?? name.Version)?.ToString() ?? "1.0.0.0",
                language: name.CultureName?.Length > 0 ? name.CultureName : "neutral",
                processorArchitecture: name.ProcessorArchitecture.ToAttributeValue() ?? "msil",
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
            var path = Settings.GeneratesLauncher ? "Launcher.exe" : EntryPointPath;

            var epe = Document.Root.GetOrAdd(AsmV2 + "entryPoint");

            var ai = epe.GetOrAdd(AsmV2 + "assemblyIdentity");
            var fi = new FileInfo(Path.Combine(FromDirectory.FullName, path));
            var name = AssemblyName.GetAssemblyName(fi.FullName);
            ai.SetAttributeValue("name", name.Name);
            ai.SetAttributeValue("version", name.Version.ToString());
            SetAssemblyAttributes(ai, name);

            var cl = epe.GetOrAdd(AsmV2 + "commandLine");
            cl.SetAttributeValue("file", path);
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

        var ps = AddPermissionSetElement(min);

        ps.SetAttributeValue("SameSite", Settings.SameSite ? "site" : null);

        var psId = ps.Attribute("ID")?.Value;

        if (psId == null)
        {
            ps.SetAttributeValue("ID", psId = "Custom");
        }

        var dar = min.GetOrAdd(AsmV2 + "defaultAssemblyRequest");
        dar.SetAttributeValue("permissionSetReference", psId);

        var el = sec.GetOrAdd(AsmV3 + "requestedPrivileges")
            .GetOrAdd(AsmV3 + "requestedExecutionLevel");
        el.SetAttributeValue("level", "asInvoker");
        el.SetAttributeValue("uiAccess", "false");
    }

    private XElement AddPermissionSetElement(XElement applicationRequestMinimum)
    {
        XElement ps;
        if (Settings.CustomPermissionSet?.Length > 0)
        {
            var source = XElement.Parse(Settings.CustomPermissionSet);

            ps = applicationRequestMinimum.GetOrAdd(AsmV2 + source.Name.LocalName);
            foreach (var a in source.Attributes())
            {
                ps.SetAttributeValue(a.Name, a.Value);
            }

            foreach (var sc in source.Elements())
            {
                var dc = ps.AddElement(AsmV2 + sc.Name.LocalName);

                foreach (var a in sc.Attributes())
                {
                    dc.SetAttributeValue(a.Name, a.Value);
                }
            }
        }
        else if (Settings.PermissionSet == PermissionSet.Internet)
        {
            ps = applicationRequestMinimum.GetOrAdd(AsmV2 + "PermissionSet");
            ps.SetAttributeValue("class", typeof(NamedPermissionSet).FullName);
            ps.SetAttributeValue("Name", "Internet");
            ps.SetAttributeValue("ID", "Custom");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(FileDialogPermission).AssemblyQualifiedName)
                .SetAttr("Access", "Open");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(FileDialogPermission).AssemblyQualifiedName)
                .SetAttr("Allowed", "ApplicationIsolationByUser")
                .SetAttr("UserQuota", "1024000");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(SecurityPermission).AssemblyQualifiedName)
                .SetAttr("Flags", "Execution");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(UIPermission).AssemblyQualifiedName)
                .SetAttr("Window", "SafeTopLevelWindows")
                .SetAttr("Clipboard", "OwnClipboard");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(PrintingPermission).AssemblyQualifiedName)
                .SetAttr("Level", "SafePrinting");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(MediaPermission).AssemblyQualifiedName)
                .SetAttr("Audio", "SafeAudio")
                .SetAttr("Video", "SafeVideo")
                .SetAttr("Image", "SafeImage");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(WebBrowserPermission).AssemblyQualifiedName)
                .SetAttr("Level", "Safe");
        }
        else if (Settings.PermissionSet == PermissionSet.LocalIntranet)
        {
            ps = applicationRequestMinimum.GetOrAdd(AsmV2 + "PermissionSet");
            ps.SetAttributeValue("class", typeof(NamedPermissionSet).FullName);
            ps.SetAttributeValue("Name", "LocalIntranet");
            ps.SetAttributeValue("ID", "Custom");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(EnvironmentPermission).AssemblyQualifiedName)
                .SetAttr("Read", "USERNAME");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(FileDialogPermission).AssemblyQualifiedName)
                .SetAttr("Unrestricted", "true");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(IsolatedStorageFilePermission).AssemblyQualifiedName)
                .SetAttr("Allowed", "AssemblyIsolationByUser")
                .SetAttr("UserQuota", long.MaxValue)
                .SetAttr("Expiry", long.MaxValue)
                .SetAttr("Permanent", "True");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(ReflectionPermission).AssemblyQualifiedName)
                .SetAttr("Flags", "ReflectionEmit, RestrictedMemberAccess");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(SecurityPermission).AssemblyQualifiedName)
                .SetAttr("Flags", "Assertion, Execution, BindingRedirects");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(UIPermission).AssemblyQualifiedName)
                .SetAttr("Unrestricted", "true");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(PrintingPermission).AssemblyQualifiedName)
                .SetAttr("Level", "DefaultPrinting");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(DnsPermission).AssemblyQualifiedName)
                .SetAttr("Unrestricted", "true");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(TypeDescriptorPermission).AssemblyQualifiedName)
                .SetAttr("Unrestricted", "true");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(MediaPermission).AssemblyQualifiedName)
                .SetAttr("Audio", "SafeAudio")
                .SetAttr("Video", "SafeVideo")
                .SetAttr("Image", "SafeImage");

            ps.AddElement(AsmV2 + "IPermission")
                .SetAttr("version", "1")
                .SetAttr("class", typeof(WebBrowserPermission).AssemblyQualifiedName)
                .SetAttr("Level", "Safe");
        }
        else
        {
            ps = applicationRequestMinimum.GetOrAdd(AsmV2 + "PermissionSet");
            ps.SetAttributeValue("Unrestricted", "true");
            ps.SetAttributeValue("ID", "Custom");
        }

        return ps;
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

    protected void AddPathElements(IEnumerable<string> paths)
    {
        List<XElement> files = null;

        var dep = Settings.GeneratesLauncher ? e => e == "Launcher.exe" : CompileMinimatch(Settings.DependentAssemblies);
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

            (files ??= new List<XElement>()).Add(CreateFileElement(p, fi));
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

    protected XElement CreateFileElement(string p, FileInfo fi)
    {
        var fe = new XElement(AsmV2 + "file");
        fe.SetAttributeValue("name", p.Replace('/', '\\'));
        fe.SetAttributeValue("size", fi.Length);
        AddHashElement(fe, fi);
        return fe;
    }

    protected override void TraceEvent(TraceEventType eventType, string message)
    {
        base.TraceEvent(eventType, message);
        Log?.Invoke(typeof(ApplicationManifestGenerator).FullName + "[" + eventType + "]: " + message);
    }

    protected override void TraceEvent(TraceEventType eventType, string format, params object[] args)
    {
        base.TraceEvent(eventType, format, args);
        Log?.Invoke(typeof(ApplicationManifestGenerator).FullName + "[" + eventType + "]: " + string.Format(format, args));
    }
}