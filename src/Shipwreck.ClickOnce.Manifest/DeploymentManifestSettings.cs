using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public class DeploymentManifestSettings : ManifestSettings
    {
        private static readonly string[] DefaultInclude
            = { "**/*.manifest" };

        public DeploymentManifestSettings()
            => Include = DefaultInclude;

        #region Description Properties

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string ApplicationManifest { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string ApplicationName { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string Publisher { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string SuiteName { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string Product { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string SupportUrl { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string ErrorReportUrl { get; set; }

        #endregion Description Properties

        #region Deployment Properties

        [DefaultValue(false)]
        [DataMember(EmitDefaultValue = false)]
        public bool Install { get; set; }

        [DefaultValue(false)]
        [DataMember(EmitDefaultValue = false)]
        public bool CreateDesktopShortcut { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string CodeBaseFolder { get; set; }

        #endregion Deployment Properties

        #region CompatibleFrameworks

        private readonly Collection<CompatibleFramework> _CompatibleFrameworks
            = new Collection<CompatibleFramework>();

        [DataMember(EmitDefaultValue = false)]
        public Collection<CompatibleFramework> CompatibleFrameworks
        {
            get => _CompatibleFrameworks;
            set
            {
                if (value != _CompatibleFrameworks)
                {
                    _CompatibleFrameworks.Clear();
                    if (value != null)
                    {
                        foreach (var c in value)
                        {
                            _CompatibleFrameworks.Add(c);
                        }
                    }
                }
            }
        }

        public bool ShouldSerializeCompatibleFrameworks()
            => _CompatibleFrameworks.Any();

        public void ResetCompatibleFrameworks()
            => _CompatibleFrameworks.Clear();

        #endregion CompatibleFrameworks

        #region Include

        public bool ShouldSerializeInclude()
            => Include.SequenceEqual(DefaultInclude);

        public void ResetInclude()
            => Include = DefaultInclude;

        #endregion Include

        #region Exclude

        public bool ShouldSerializeExclude()
            => Exclude.Any();

        public void ResetExclude()
            => Exclude.Clear();

        #endregion Exclude
    }
}