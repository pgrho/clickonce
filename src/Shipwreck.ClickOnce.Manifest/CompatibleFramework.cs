using System.ComponentModel;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public class CompatibleFramework
    {
        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string TargetVersion { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string Profile { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string SupportedRuntime { get; set; }

        public override bool Equals(object obj)
            => obj is CompatibleFramework o
               && o.TargetVersion == TargetVersion
               && o.Profile == Profile
               && o.SupportedRuntime == SupportedRuntime;

        public override int GetHashCode()
            => (TargetVersion?.GetHashCode() ?? 0)
               ^ (Profile?.GetHashCode() ?? 0)
               ^ (SupportedRuntime?.GetHashCode() ?? 0);

        public CompatibleFramework Clone()
            => new()
            {
                TargetVersion = TargetVersion,
                Profile = Profile,
                SupportedRuntime = SupportedRuntime
            };
    }
}