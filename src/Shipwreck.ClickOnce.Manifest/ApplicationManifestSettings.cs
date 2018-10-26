using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Shipwreck.ClickOnce.Manifest
{
    public class ApplicationManifestSettings
    {
        private static readonly string[] DefaultInclude
            = { "**" };

        private static readonly string[] DefaultExclude
            = { @"**/*.pdb", "app.publish/**" };

        private readonly Collection<string> _Include;

        private readonly Collection<string> _Exclude;

        public ApplicationManifestSettings()
        {
            _Include = new Collection<string>();
            foreach (var s in DefaultInclude)
            {
                _Include.Add(s);
            }

            _Exclude = new Collection<string>();
            foreach (var s in DefaultExclude)
            {
                _Exclude.Add(s);
            }
        }

        [DefaultValue(null)]
        public string FromDirectory { get; set; }

        [DefaultValue(null)]
        public string ToDirectory { get; set; }

        [DefaultValue(null)]
        public string EntryPoint { get; set; }

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
    }
}