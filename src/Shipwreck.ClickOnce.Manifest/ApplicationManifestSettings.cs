using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public class ApplicationManifestSettings : ManifestSettings
    {
        public ApplicationManifestSettings()
        {
            Include = DefaultInclude;
            Exclude = DefaultExclude;
            DependentAssemblies = DefaultDependentAssemblies;
        }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string EntryPoint { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string IconFile { get; set; }

        #region Include

        internal static readonly string[] DefaultInclude
            = { "**" };

        public override bool ShouldSerializeInclude()
            => Include.SequenceEqual(DefaultInclude);

        public override void ResetInclude()
            => Include = DefaultInclude;

        #endregion Include

        #region Exclude

        internal static readonly string[] DefaultExclude
            = { @"**/*.pdb", "**/*.application", "app.publish/**" };

        public override bool ShouldSerializeExclude()
            => Exclude.SequenceEqual(DefaultExclude);

        public override void ResetExclude()
            => Exclude = DefaultExclude;

        #endregion Exclude

        #region DependentAssemblies

        internal static readonly string[] DefaultDependentAssemblies
            = { @"**/*.exe", "**/*.dll" };

        public override bool ShouldSerializeDependentAssemblies()
            => DependentAssemblies.SequenceEqual(DefaultDependentAssemblies);

        public override void ResetDependentAssemblies()
            => DependentAssemblies = DefaultDependentAssemblies;

        #endregion DependentAssemblies
    }
}