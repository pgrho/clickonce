using System.Collections.Generic;

namespace Shipwreck.ClickOnce.Manifest
{
    public static class ManifestSettingsExtensions
    {
        #region Include

        public static T AddInclude<T>(this T settings, string pattern)
            where T : ManifestSettings
        {
            settings.Include.Add(pattern);
            return settings;
        }

        public static T AddIncludes<T>(this T settings, IEnumerable<string> patterns)
            where T : ManifestSettings
        {
            foreach (var p in patterns)
            {
                settings.Include.Add(p);
            }

            return settings;
        }

        public static T AddIncludes<T>(this T settings, params string[] patterns)
            where T : ManifestSettings
            => settings.AddIncludes((IEnumerable<string>)patterns);

        public static T RemoveInclude<T>(this T settings, string pattern)
            where T : ManifestSettings
        {
            settings.Include.Remove(pattern);
            return settings;
        }

        public static T RemoveIncludes<T>(this T settings, IEnumerable<string> patterns)
            where T : ManifestSettings
        {
            foreach (var p in patterns)
            {
                settings.Include.Remove(p);
            }

            return settings;
        }

        public static T RemoveIncludes<T>(this T settings, params string[] patterns)
            where T : ManifestSettings
            => settings.RemoveIncludes((IEnumerable<string>)patterns);

        public static T ClearIncludes<T>(this T settings)
            where T : ManifestSettings
        {
            settings.Include.Clear();
            return settings;
        }

        #endregion Include

        #region Exclude

        public static T AddExclude<T>(this T settings, string pattern)
            where T : ManifestSettings
        {
            settings.Exclude.Add(pattern);
            return settings;
        }

        public static T AddExcludes<T>(this T settings, IEnumerable<string> patterns)
            where T : ManifestSettings
        {
            foreach (var p in patterns)
            {
                settings.Exclude.Add(p);
            }

            return settings;
        }

        public static T AddExcludes<T>(this T settings, params string[] patterns)
            where T : ManifestSettings
            => settings.AddExcludes((IEnumerable<string>)patterns);

        public static T RemoveExclude<T>(this T settings, string pattern)
            where T : ManifestSettings
        {
            settings.Exclude.Remove(pattern);
            return settings;
        }

        public static T RemoveExcludes<T>(this T settings, IEnumerable<string> patterns)
            where T : ManifestSettings
        {
            foreach (var p in patterns)
            {
                settings.Exclude.Remove(p);
            }

            return settings;
        }

        public static T RemoveExcludes<T>(this T settings, params string[] patterns)
            where T : ManifestSettings
            => settings.RemoveExcludes((IEnumerable<string>)patterns);

        public static T ClearExcludes<T>(this T settings)
            where T : ManifestSettings
        {
            settings.Exclude.Clear();
            return settings;
        }

        #endregion Exclude

        #region DependentAssemblies

        public static T AddDependentAssembly<T>(this T settings, string pattern)
            where T : ManifestSettings
        {
            settings.DependentAssemblies.Add(pattern);
            return settings;
        }

        public static T AddDependentAssemblies<T>(this T settings, IEnumerable<string> patterns)
            where T : ManifestSettings
        {
            foreach (var p in patterns)
            {
                settings.DependentAssemblies.Add(p);
            }

            return settings;
        }

        public static T AddDependentAssemblies<T>(this T settings, params string[] patterns)
            where T : ManifestSettings
            => settings.AddDependentAssemblies((IEnumerable<string>)patterns);

        public static T RemoveDependentAssembly<T>(this T settings, string pattern)
            where T : ManifestSettings
        {
            settings.DependentAssemblies.Remove(pattern);
            return settings;
        }

        public static T RemoveDependentAssemblies<T>(this T settings, IEnumerable<string> patterns)
            where T : ManifestSettings
        {
            foreach (var p in patterns)
            {
                settings.DependentAssemblies.Remove(p);
            }

            return settings;
        }

        public static T RemoveDependentAssemblies<T>(this T settings, params string[] patterns)
            where T : ManifestSettings
            => settings.RemoveDependentAssemblies((IEnumerable<string>)patterns);

        public static T ClearDependentAssemblies<T>(this T settings)
            where T : ManifestSettings
        {
            settings.DependentAssemblies.Clear();
            return settings;
        }

        #endregion DependentAssemblies

        #region CompatibleFrameworks

        public static T AddCompatibleFramework<T>(this T settings, CompatibleFramework framework)
            where T : DeploymentManifestSettings
        {
            settings.CompatibleFrameworks.Add(framework);
            return settings;
        }

        public static T AddCompatibleFrameworks<T>(this T settings, IEnumerable<CompatibleFramework> frameworks)
            where T : DeploymentManifestSettings
        {
            foreach (var p in frameworks)
            {
                settings.CompatibleFrameworks.Add(p);
            }

            return settings;
        }

        public static T AddCompatibleFrameworks<T>(this T settings, params CompatibleFramework[] frameworks)
            where T : DeploymentManifestSettings
            => settings.AddCompatibleFrameworks((IEnumerable<CompatibleFramework>)frameworks);

        public static T RemoveCompatibleFramework<T>(this T settings, CompatibleFramework framework)
            where T : DeploymentManifestSettings
        {
            settings.CompatibleFrameworks.Remove(framework);
            return settings;
        }

        public static T RemoveCompatibleFrameworks<T>(this T settings, IEnumerable<CompatibleFramework> frameworks)
            where T : DeploymentManifestSettings
        {
            foreach (var p in frameworks)
            {
                settings.CompatibleFrameworks.Remove(p);
            }

            return settings;
        }

        public static T RemoveCompatibleFrameworks<T>(this T settings, params CompatibleFramework[] frameworks)
            where T : DeploymentManifestSettings
            => settings.RemoveCompatibleFrameworks((IEnumerable<CompatibleFramework>)frameworks);

        public static T ClearCompatibleFrameworks<T>(this T settings)
            where T : DeploymentManifestSettings
        {
            settings.CompatibleFrameworks.Clear();
            return settings;
        }

        #endregion CompatibleFrameworks
    }
}