using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public class ApplicationManifestSettings : ManifestSettings
    {
        private static readonly string[] DefaultInclude
            = { "**" };

        private static readonly string[] DefaultExclude
            = { @"**/*.pdb", "**/*.application", "app.publish/**" };

        public ApplicationManifestSettings()
        {
            Include = DefaultInclude;
            Exclude = DefaultExclude;
        }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string EntryPoint { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string IconFile { get; set; }

        #region Include

        public bool ShouldSerializeInclude()
            => Include.SequenceEqual(DefaultInclude);

        public void ResetInclude()
            => Include = DefaultInclude;

        #endregion Include

        #region Exclude

        public bool ShouldSerializeExclude()
            => Exclude.SequenceEqual(DefaultExclude);

        public void ResetExclude()
            => Exclude = DefaultExclude;

        #endregion Exclude
    }
}