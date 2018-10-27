using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public class ApplicationManifestSettings
    {
        private static readonly string[] DefaultInclude
            = { "**" };

        private static readonly string[] DefaultExclude
            = { @"**/*.pdb", "**/*.application", "app.publish/**" };

        public ApplicationManifestSettings()
        {
            _Include = new Collection<string>();
            Include = DefaultInclude;

            _Exclude = new Collection<string>();
            Exclude = DefaultExclude;
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
        public string EntryPoint { get; set; }

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

        public bool ShouldSerializeInclude()
            => _Include.SequenceEqual(DefaultInclude);

        public void ResetInclude()
            => Include = DefaultInclude;

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

        public bool ShouldSerializeExclude()
            => _Exclude.SequenceEqual(DefaultExclude);

        public void ResetExclude()
            => Exclude = DefaultExclude;

        #endregion Exclude
    }
}