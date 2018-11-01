using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public enum PermissionSet
    {
        [EnumMember]
        FullTrust,

        [EnumMember]
        LocalIntranet,

        [EnumMember]
        Internet,
    }
}