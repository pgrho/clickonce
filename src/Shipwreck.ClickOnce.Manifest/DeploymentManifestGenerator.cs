namespace Shipwreck.ClickOnce.Manifest
{
    public class DeploymentManifestGenerator : ManifestGenerator
    {
        public DeploymentManifestGenerator(DeploymentManifestSettings settings)
            : base(settings)
        { }

        protected new DeploymentManifestSettings Settings
            => (DeploymentManifestSettings)base.Settings;
    }
}