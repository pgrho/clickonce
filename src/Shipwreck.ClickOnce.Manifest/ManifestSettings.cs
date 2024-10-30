﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace Shipwreck.ClickOnce.Manifest;

[DataContract]
[KnownType(typeof(ApplicationManifestSettings))]
[KnownType(typeof(DeploymentManifestSettings))]
public abstract class ManifestSettings
{
    internal const bool DEFAULT_GENERATES_LAUNCHER
#if NET8_0_OR_GREATER
        = true;
#else
        = false;
#endif
    internal const string DEFAULT_VISUAL_STUDIO_VERSION = "17";

    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public string FromDirectory { get; set; }

    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public string ToDirectory { get; set; }

    [DefaultValue(false)]
    [DataMember(EmitDefaultValue = false)]
    public bool DeleteDirectory { get; set; }

    [DefaultValue(false)]
    [DataMember(EmitDefaultValue = false)]
    public bool Overwrite { get; set; }

    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public Version Version { get; set; }

    [DefaultValue(true)]
    [DataMember(EmitDefaultValue = false)]
    public bool IncludeHash { get; set; } = true;

    [DefaultValue(true)]
    [DataMember(EmitDefaultValue = false)]
    public bool MapFileExtensions { get; set; } = true;

    #region Certificate

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public X509Certificate2 Certificate { get; set; }

    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public string CertificateFileName { get; set; }

    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public byte[] CertificateRawData { get; set; }

    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public string CertificatePassword { get; set; }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SecureString CertificateSecurePassword { get; set; }

    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public string CertificateThumbprint { get; set; }

    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public string TimestampUrl { get; set; }

    internal const int DEFAULT_MAX_PASSWORD_RETRY_COUNT = 10;

    [DefaultValue(DEFAULT_MAX_PASSWORD_RETRY_COUNT)]
    [DataMember(EmitDefaultValue = false)]
    public int MaxPasswordRetryCount { get; set; } = DEFAULT_MAX_PASSWORD_RETRY_COUNT;

    #endregion Certificate

    #region Include

    private Collection<string> _Include;

    [DataMember(EmitDefaultValue = false)]
    public IList<string> Include
    {
        get => CollectionHelper.GetOrCreate(ref _Include);
        set => CollectionHelper.Set(ref _Include, value);
    }

    public virtual bool ShouldSerializeInclude()
        => Include.Any();

    public virtual void ResetInclude()
        => Include.Clear();

    #endregion Include

    #region Exclude

    private Collection<string> _Exclude;

    [DataMember(EmitDefaultValue = false)]
    public IList<string> Exclude
    {
        get => CollectionHelper.GetOrCreate(ref _Exclude);
        set => CollectionHelper.Set(ref _Exclude, value);
    }

    public virtual bool ShouldSerializeExclude()
        => Exclude.Any();

    public virtual void ResetExclude()
        => Exclude.Clear();

    #endregion Exclude

    #region DependentAssemblies

    private Collection<string> _DependentAssemblies;

    [DataMember(EmitDefaultValue = false)]
    public IList<string> DependentAssemblies
    {
        get => CollectionHelper.GetOrCreate(ref _DependentAssemblies);
        set => CollectionHelper.Set(ref _DependentAssemblies, value);
    }

    public virtual bool ShouldSerializeDependentAssemblies()
        => _DependentAssemblies?.Count > 0;

    public virtual void ResetDependentAssemblies()
        => _DependentAssemblies?.Clear();

    #endregion DependentAssemblies
}