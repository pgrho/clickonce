# Shipwreck.ClickOnce.Manifest

ClickOnce Manifest Generator.

## Usage

- [NuGet](https://www.nuget.org/packages/Shipwreck.ClickOnce.Manifest)

1. Choose generator.
  - `ApplicationManifestGenerator`: Copies application files and generates `***.exe.manifest`.
  - `DeploymentManifestGenerator`: Generates `***.application`.
  - `ApplicationPublisher`: Executes `ApplicationManifestGenerator` and `DeploymentManifestGenerator`.
2. Instantiate and configure corresponding settings object.
  - `ApplicationManifestSettings`
  - `DeploymentManifestSettings`
  - `PublishSettings`
3. Instantiate generator and invoke `.Generate()`

### Example

Generate an application manifest by a build result and copy application files.
```
// using Shipwreck.ClickOnce.Manifest;

new ApplicationManifestGenerator(new ApplicationManifestSettings()
{
    FromDirectory = "bin/Release",
    ToDirectory = "publish/Application Files/TestApp_1_2_3_4",
    Version = new Version(1, 2, 3, 4),
}).Generate();
```

And then generate a deployment manifest from the application manifest.

```
new DeploymentManifestGenerator(new DeploymentManifestSettings()
{
    FromDirectory = "publish/Application Files/TestApp_1_2_3_4",
    ToDirectory = "publish",
    Version = new Version(1, 2, 3, 4),

    ApplicationName = "TestApp",

    Install = true,
}).Generate();
```

### Example (`ApplicationPublisher`)

```
// using Shipwreck.ClickOnce.Manifest;

new ApplicationPublisher(new ApplicationManifestSettings()
{
    FromDirectory = "bin/Release",
    ToDirectory = "publish",
    Version = new Version(1, 2, 3, 4),

    ApplicationName = "TestApp",

    Install = true,
}).Generate();
```

## Settings

### Common

|Property|Type|Description|Default|
|-|-|-|-|
|`FromDirectory`|`string`|A directory that contains application file||
|`ToDirectory`|`string`|A directory to output the manifest. `ApplicationManifestGenerator` also copies Application files||
|`DeleteDirectory`|`bool`|A value indicating whether the existing output directory should be deleted or not.|`false`|
|`Overwrite`|`bool`|A value indicating whether the existing file should be overwritten or not.|`false`|
|`Version`|`System.Version`|A version of the application.|Use version of the entry point assembly or application manifest.|
|`Include`|`IList<string>`|Path patterns to specify files included in the application.|(See concrete type)|
|`Exclude`|`IList<string>`|Path patterns to specify files excluded from the application.|(See concrete type)|
|`DependentAssemblies`|`IList<string>`|Path patterns to specify files that are a .NET Assembly.|(See concrete type)|
|`IncludeHash`|`bool`|A value indicating whether hash elements will be generated or not.|`true`|
|`MapFileExtensions`|`bool`|A value indicating whether the application files should be added `.deploy` extension.|`true`|
|`CertificateFileName`|`string`|Path of a `.pfx` certificate to sign the manifest.||
|`CertificatePassword`|`string`|The password of the `.pfx`.||
|`CertificateThumbprint`|`string`|The thumbprint of a certificate to sign the manifest.||
|`TimestampUrl`|`string`|URL of the timestamp server.||

### Application Manifest

|Property|Type|Description|Default|
|-|-|-|-|
|`Include`|`IList<string>`|(Inherited)|`["**"]`|
|`Exclude`|`IList<string>`|(Inherited)|`["**/*.pdb", "**/*.application", "app.publish/**"]`|
|`DependentAssemblies`|`IList<string>`|(Inherited)|`["**/*.exe", "**/*.dll"]`|
|`EntryPoint`|`string`|A relative path to the entry point assembly.|(see below)|
|`IconFile`|`string`|A relative path to the application icon.|(see below)|
|`PermissionSet`|`PermissionSet`|Builtin `PermissionSet` applied to the application. (`FullTrust`/`LocalIntranet`/`Internet`)|`FullTrust`|
|`SameSite`|`bool`|A value indicating whether the application has `WebPermission` to same origin.|false|
|`CustomPermissionSet`|`string`|XML string that represents the custom `PermissionSet` applied to the application.||


#### EntryPoint detection precedence.
1. The first `.exe` file that has `.exe.manifest`.
2. The first `.exe` file

#### IconFile detection precedence.
1. `{EntryPoint}.ico`
2. The first `.ico` file

### Deployment Manifest

|Property|Type|Description|Default|
|-|-|-|-|
|`Include`|`IList<string>`|(Inherited. Should not change)|`["**/*.manifest"]`|
|`Exclude`|`IList<string>`|(Inherited. Should not change)|`[]`|
|`DependentAssemblies`|`IList<string>`|(Inherited. Should not change)|`[]`|
|`ApplicationManifest`|`string`|A relative path to the apllication manifest|The first `.exe.manifest` file|
|`ApplicationName`|`string`|The identifier of the ClickOnce application.|Use the application manifest value.|
|`Publisher`|`string`|The name of the publisher.||
|`SuiteName`|`string`|The name of the suite.||
|`Product`|`string`|The name of the product. Will be used by the shortcut.||
|`SupportUrl`|`string`|A URL for the support.||
|`ErrorReportUrl`|`string`|A URL for the error reporting.||
|`Install`|`bool`|A value indicating whether the application can be used in offline or not.|`false`|
|`CreateDesktopShortcut`|`bool`|A value indicating whether the application shortcut will be created on the user's Desktop or not .|`false`|
|`MinimumRequiredVersion`|`System.Version`|A minimum version that is required to run the application.||
|`UpdateAfterStartup`|`bool`|A value indicating whether the application will check for newer version after startup or not .|`false`|
|`MaximumAge`|`int`|Interval of checking update after startup. `0` to check every time.|`0`|
|`MaximumAgeUnit`|`AgeUnit`|The unit of `MaximumAge`. (`Hours`/`Days`/`Weeks`)|`Days`|
|`UpdateBeforeStartup`|`bool`|A value indicating whether the application will check for newer version before startup or not .|`false`|
|`CompatibleFrameworks`|`IList<CompatibleFramework>`|List of the compatible frameworks|Detected by `.exe.config`'s `<startup>` element.|

## License

MIT