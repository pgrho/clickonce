using System.ComponentModel;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest;

[DataContract]
public class FileAssociation
{
    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public string Extension { get; set; }

    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public string Description { get; set; }

    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public string ProgId { get; set; }

    [DefaultValue(null)]
    [DataMember(EmitDefaultValue = false)]
    public string DefaultIcon { get; set; }
}