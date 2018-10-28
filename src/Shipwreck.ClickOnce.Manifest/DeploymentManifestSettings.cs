using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public class DeploymentManifestSettings : ManifestSettings
    {
        internal static readonly string[] DefaultInclude
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

        private Collection<CompatibleFramework> _CompatibleFrameworks;

        [DataMember(EmitDefaultValue = false)]
        public IList<CompatibleFramework> CompatibleFrameworks
        {
            get => _CompatibleFrameworks ?? (_CompatibleFrameworks = new Collection<CompatibleFramework>());
            set
            {
                if (value != _CompatibleFrameworks)
                {
                    _CompatibleFrameworks?.Clear();
                    if (value != null)
                    {
                        foreach (var c in value)
                        {
                            CompatibleFrameworks.Add(c);
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

        public override bool ShouldSerializeInclude()
            => Include.SequenceEqual(DefaultInclude);

        public override void ResetInclude()
            => Include = DefaultInclude;

        #endregion Include
    }
}