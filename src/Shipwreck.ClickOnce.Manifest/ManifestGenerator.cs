﻿using Microsoft.Build.Tasks.Deployment.ManifestUtilities;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Shipwreck.ClickOnce.Manifest;

public abstract class ManifestGenerator
{
    protected internal static readonly TraceSource TraceSource
        = new(typeof(ApplicationManifestGenerator).Namespace);

    protected Action<string> Log { get; set; }

    protected internal static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    protected internal static readonly XNamespace AsmV1 = "urn:schemas-microsoft-com:asm.v1";
    protected internal static readonly XNamespace AsmV2 = "urn:schemas-microsoft-com:asm.v2";
    protected internal static readonly XNamespace AsmV3 = "urn:schemas-microsoft-com:asm.v3";
    protected internal static readonly XNamespace Dsig = "http://www.w3.org/2000/09/xmldsig#";

    protected internal static readonly XNamespace ClickOnceV1 = "urn:schemas-microsoft-com:clickonce.v1";

    protected internal static readonly XNamespace ClickOnceV2 = "urn:schemas-microsoft-com:clickonce.v2";

    protected ManifestGenerator(ManifestSettings settings)
    {
        Settings = settings;
    }

    protected ManifestSettings Settings { get; }

    #region Input Properties

    #region FromDirectory

    private DirectoryInfo _FromDirectory;

    public DirectoryInfo FromDirectory
        => _FromDirectory ??= new DirectoryInfo(Settings.FromDirectory?.Length > 0 ? Settings.FromDirectory : Environment.CurrentDirectory);

    #endregion FromDirectory

    #region FromDirectoryUri

    private Uri _FromDirectoryUri;

    protected internal Uri FromDirectoryUri
        => _FromDirectoryUri ??= new Uri(FromDirectory.FullName.Trim('/', '\\') + '\\');

    #endregion FromDirectoryUri

    #region IncludedFilePaths

    private List<string> _IncludedFilePaths;
    public List<string> IncludedFilePaths => _IncludedFilePaths ??= GetIncludedFilePaths();

    private List<string> GetIncludedFilePaths()
    {
        TraceInformation("Searching files from {0}", FromDirectory.FullName);

        var include = CompileMinimatch(Settings.Include);
        var exclude = CompileMinimatch(Settings.Exclude);

        var paths = new List<string>();

        foreach (var f in FromDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            var fu = new Uri(f.FullName);
            var path = Uri.UnescapeDataString(FromDirectoryUri.MakeRelativeUri(fu).ToString());

            if (include(path) && !exclude(path))
            {
                TraceEvent(TraceEventType.Verbose, "Found: {0}", path);

                paths.Add(path);
            }
        }

        return paths;
    }

    #endregion IncludedFilePaths

    #region ManifestPath

    private string _ManifestPath;

    public string ManifestPath
        => _ManifestPath ??= GetManifestPath();

    protected abstract string GetManifestPath();

    #endregion ManifestPath

    #endregion Input Properties

    #region Output Properties

    #region Document

    private XDocument _Document;

    public XDocument Document => _Document ??= CopyOrCreateManifestDocument();

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
        => _ToDirectory ??= new DirectoryInfo(Settings.ToDirectory?.Length > 0 ? Path.GetFullPath(Settings.ToDirectory) : FromDirectory.FullName);

    #endregion ToDirectory

    #region ToDirectoryUri

    private Uri _ToDirectoryUri;
    protected internal Uri ToDirectoryUri => _ToDirectoryUri ??= new Uri(ToDirectory.FullName.Trim('/', '\\') + "\\");

    #endregion ToDirectoryUri

    private string _OutputFileName;

    protected string OutputFileName
        => _OutputFileName ??= GetOutputFileName();

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
            using var sha = SHA256.Create();
            h.AddElement(Dsig + "DigestValue").Value = Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(f.FullName)));
        }
    }

    #endregion GeneratePathElements

    protected virtual void CopyFiles()
    {
        if (!FromDirectory.FullName.Equals(ToDirectory.FullName, StringComparison.InvariantCultureIgnoreCase))
        {
            if (Settings.DeleteDirectory && ToDirectory.Exists)
            {
                TraceInformation("Removing Directory :{0}", ToDirectory.FullName);
                ToDirectory.Delete(true);
            }

            foreach (var p in IncludedFilePaths)
            {
                var dest = new FileInfo(new Uri(ToDirectoryUri, p + (Settings.MapFileExtensions ? ".deploy" : null)).LocalPath);
                if (!dest.Directory.Exists)
                {
                    TraceInformation("Creating Directory :{0}", dest.Directory.FullName);
                    dest.Directory.Create();
                }

                TraceInformation("Copying file :{0}", p);
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
            TraceInformation("Creating Directory :{0}", d.FullName);
            d.Create();
        }
        TraceInformation("Writing Manifest to {0}", p);
        TraceEvent(TraceEventType.Verbose, "Manifest Content: {0}", Document);
        Document.Save(p);

        var tu = Settings.TimestampUrl?.Length > 0 ? new Uri(Settings.TimestampUrl) : null;
        if (Settings.CertificateThumbprint?.Length > 0)
        {
            SecurityUtilities.SignFile(Settings.CertificateThumbprint, tu, p);
        }
        else
        {
            var cert = GetCertificate();
            if (cert != null)
            {
                SecurityUtilities.SignFile(cert, tu, p);
            }
        }
    }

    private X509Certificate2 GetCertificate()
    {
        if (Settings.Certificate != null)
        {
            return Settings.Certificate;
        }
        const X509KeyStorageFlags flags = X509KeyStorageFlags.PersistKeySet;

        for (var i = 1; ; i++)
        {
            try
            {
                if (Settings.CertificateFileName?.Length > 0)
                {
                    if (Settings.CertificateSecurePassword != null)
                    {
                        return new X509Certificate2(
                            Settings.CertificateFileName,
                            Settings.CertificateSecurePassword,
                            flags);
                    }
                    return new X509Certificate2(
                        Settings.CertificateFileName,
                        Settings.CertificatePassword,
                        flags);
                }
                else if (Settings.CertificateRawData?.Length > 0)
                {
                    if (Settings.CertificateSecurePassword != null)
                    {
                        return new X509Certificate2(
                            Settings.CertificateRawData,
                            Settings.CertificateSecurePassword,
                            flags);
                    }
                    return new X509Certificate2(
                        Settings.CertificateRawData,
                        Settings.CertificatePassword,
                        flags);
                }
                return null;
            }
            catch (CryptographicException ex)
            {
                var retry = i < Settings.MaxPasswordRetryCount;
                TraceEvent(
                    retry ? TraceEventType.Warning : TraceEventType.Error,
                    "An Exception was caught while Opening the certificate: {0}",
                    ex);
                if (retry)
                {
                    continue;
                }
                throw;
            }
        }
    }

    private static readonly Regex _IconPattern
        = new(@"^[^/]+\.ico$", RegexOptions.IgnoreCase);

    protected static bool IsIco(string p)
        => _IconPattern.IsMatch(p);

    internal static Func<string, bool> CompileMinimatch(IEnumerable<string> patterns)
        => new Shipwreck.Minimatch.MatcherFactory()
        {
            AllowBackslash = true,
            IgnoreCase = true
        }.Compile(patterns);

    protected void TraceInformation(string message) => TraceEvent(TraceEventType.Information, message);
    protected void TraceInformation(string format, params object[] args) => TraceEvent(TraceEventType.Information, format, args);
    protected void TraceError(string message) => TraceEvent(TraceEventType.Error, message);
    protected void TraceError(string format, params object[] args) => TraceEvent(TraceEventType.Error, format, args);

    protected virtual void TraceEvent(TraceEventType eventType, string message)
        => TraceSource?.TraceEvent(eventType, 0, message);

    protected virtual void TraceEvent(TraceEventType eventType, string format, params object[] args)
        => TraceSource?.TraceEvent(eventType, 0, format, args);
}