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
            DependentAssemblies = ApplicationManifestSettings.DefaultDependentAssemblies;
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

        #region DependentAssemblies

        public override bool ShouldSerializeDependentAssemblies()
            => DependentAssemblies.SequenceEqual(ApplicationManifestSettings.DefaultDependentAssemblies);

        public override void ResetDependentAssemblies()
            => DependentAssemblies = ApplicationManifestSettings.DefaultDependentAssemblies;

        #endregion DependentAssemblies
    }
}