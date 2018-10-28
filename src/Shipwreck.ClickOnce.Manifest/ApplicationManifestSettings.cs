﻿using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public class ApplicationManifestSettings : ManifestSettings
    {
        internal static readonly string[] DefaultInclude
            = { "**" };

        internal static readonly string[] DefaultExclude
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

        public override bool ShouldSerializeInclude()
            => Include.SequenceEqual(DefaultInclude);

        public override void ResetInclude()
            => Include = DefaultInclude;

        #endregion Include

        #region Exclude

        public override bool ShouldSerializeExclude()
            => Exclude.SequenceEqual(DefaultExclude);

        public override void ResetExclude()
            => Exclude = DefaultExclude;

        #endregion Exclude
    }
}