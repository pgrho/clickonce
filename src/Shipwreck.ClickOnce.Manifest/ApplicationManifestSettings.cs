using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Shipwreck.ClickOnce.Manifest
{
    [DataContract]
    public class ApplicationManifestSettings : ManifestSettings, IFileAssociationSettings
    {
        public ApplicationManifestSettings()
        {
            Include = DefaultInclude;
            Exclude = DefaultExclude;
            DependentAssemblies = DefaultDependentAssemblies;
        }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string EntryPoint { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string IconFile { get; set; }

        [DefaultValue(DEFAULT_GENERATES_LAUNCHER)]
        [DataMember(EmitDefaultValue = false)]
        public bool GeneratesLauncher { get; set; } = DEFAULT_GENERATES_LAUNCHER;

        [DefaultValue(DEFAULT_VISUAL_STUDIO_VERSION)]
        [DataMember(EmitDefaultValue = false)]
        public string VisualStudioVersion { get; set; } = DEFAULT_VISUAL_STUDIO_VERSION;

        #region PermissionSet

        [DefaultValue(PermissionSet.FullTrust)]
        [DataMember(EmitDefaultValue = false)]
        public PermissionSet PermissionSet { get; set; } = PermissionSet.FullTrust;

        [DefaultValue(false)]
        [DataMember(EmitDefaultValue = false)]
        public bool SameSite { get; set; }

        [DefaultValue(null)]
        [DataMember(EmitDefaultValue = false)]
        public string CustomPermissionSet { get; set; }

#endregion PermissionSet

#region Include

        internal static readonly string[] DefaultInclude
            = { "**" };

        public override bool ShouldSerializeInclude()
            => Include.SequenceEqual(DefaultInclude);

        public override void ResetInclude()
            => Include = DefaultInclude;

#endregion Include

#region Exclude

        internal static readonly string[] DefaultExclude
            = { @"**/*.pdb", "**/*.application", "app.publish/**" };

        public override bool ShouldSerializeExclude()
            => Exclude.SequenceEqual(DefaultExclude);

        public override void ResetExclude()
            => Exclude = DefaultExclude;

#endregion Exclude

#region DependentAssemblies

        internal static readonly string[] DefaultDependentAssemblies
            = { @"**/*.exe", "**/*.dll" };

        public override bool ShouldSerializeDependentAssemblies()
            => DependentAssemblies.SequenceEqual(DefaultDependentAssemblies);

        public override void ResetDependentAssemblies()
            => DependentAssemblies = DefaultDependentAssemblies;

#endregion DependentAssemblies

#region FileAssociations

        private Collection<FileAssociation> _FileAssociations;

        [DataMember(EmitDefaultValue = false)]
        public IList<FileAssociation> FileAssociations
        {
            get => CollectionHelper.GetOrCreate(ref _FileAssociations);
            set => CollectionHelper.Set(ref _FileAssociations, value);
        }

        public bool ShouldSerializeFileAssociations()
            => _FileAssociations?.Count > 0;

        public void ResetFileAssociations()
            => _FileAssociations?.Clear();

#endregion FileAssociations
    }
}