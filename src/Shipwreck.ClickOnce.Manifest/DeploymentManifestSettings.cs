using System.Linq;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public class DeploymentManifestSettings : ManifestSettings
    {
        private static readonly string[] DefaultInclude
            = { "**/*.manifest" };

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