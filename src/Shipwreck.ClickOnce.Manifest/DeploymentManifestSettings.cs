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