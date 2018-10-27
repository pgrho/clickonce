using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public abstract class ManifestSettings
    {
        public ManifestSettings()
        {
            _Include = new Collection<string>();
            _Exclude = new Collection<string>();
        }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string FromDirectory { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string ToDirectory { get; set; }

        [DefaultValue(false)]
        [DataMember(EmitDefaultValue = false)]
        public bool DeleteDirectory { get; set; }

        [DefaultValue(false)]
        [DataMember(EmitDefaultValue = false)]
        public bool Overwrite { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public Version Version { get; set; }

        #region Include

        private readonly Collection<string> _Include;

        [DataMember(EmitDefaultValue = false)]
        public IList<string> Include
        {
            get => _Include;
            set
            {
                if (_Include != value)
                {
                    _Include.Clear();
                    foreach (var s in value)
                    {
                        _Include.Add(s);
                    }
                }
            }
        }

        #endregion Include

        #region Exclude

        private readonly Collection<string> _Exclude;

        [DataMember(EmitDefaultValue = false)]
        public IList<string> Exclude
        {
            get => _Exclude;
            set
            {
                if (_Exclude != value)
                {
                    _Exclude.Clear();
                    foreach (var s in value)
                    {
                        _Exclude.Add(s);
                    }
                }
            }
        }

        #endregion Exclude
    }
}