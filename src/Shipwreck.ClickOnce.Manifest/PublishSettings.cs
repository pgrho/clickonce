using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public class PublishSettings : DeploymentManifestSettings
    {
        public PublishSettings()
        {
            Include = ApplicationManifestSettings.DefaultInclude;
            Exclude = ApplicationManifestSettings.DefaultExclude;
        }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string EntryPoint { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string IconFile { get; set; }

        #region Include

        public override bool ShouldSerializeInclude()
            => Include.SequenceEqual(ApplicationManifestSettings.DefaultInclude);

        public override void ResetInclude()
            => Include = ApplicationManifestSettings.DefaultInclude;

        #endregion Include

        #region Exclude

        public override bool ShouldSerializeExclude()
            => Exclude.SequenceEqual(ApplicationManifestSettings.DefaultExclude);

        public override void ResetExclude()
            => Exclude = ApplicationManifestSettings.DefaultExclude;

        #endregion Exclude
    }
}