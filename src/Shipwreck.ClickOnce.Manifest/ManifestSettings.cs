using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    [KnownType(typeof(ApplicationManifestSettings))]
    [KnownType(typeof(DeploymentManifestSettings))]
    public abstract class ManifestSettings
    {
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

        #region Certificate

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string CertificateFileName { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string CertificatePassword { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string CertificateThumbprint { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string TimestampUrl { get; set; }

        #endregion Certificate

        #region Include

        private Collection<string> _Include;

        [DataMember(EmitDefaultValue = false)]
        public IList<string> Include
        {
            get => _Include ?? (_Include = new Collection<string>());
            set
            {
                if (_Include != value)
                {
                    _Include?.Clear();
                    if (value != null)
                    {
                        foreach (var s in value)
                        {
                            Include.Add(s);
                        }
                    }
                }
            }
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
            get => _Exclude ?? (_Exclude = new Collection<string>());
            set
            {
                if (_Exclude != value)
                {
                    _Exclude?.Clear();
                    if (value != null)
                    {
                        foreach (var s in value)
                        {
                            Exclude.Add(s);
                        }
                    }
                }
            }
        }

        public virtual bool ShouldSerializeExclude()
            => Exclude.Any();

        public virtual void ResetExclude()
            => Exclude.Clear();

        #endregion Exclude
    }
}