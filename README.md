# Shipwreck.ClickOnce.Manifest

ClickOnce Manifest Generator.

## Usage

1. Choose generator.
  - `ApplicationManifestGenerator`: Copies application files and generates `***.exe.manifest`.
  - `DeploymentManifestGenerator`: Generates `***.application`.
  - `ApplicationPublisher`: Executes `ApplicationManifestGenerator` and `DeploymentManifestGenerator`.
2. Instantiate and configure corresponding settings object.
  - `ApplicationManifestSettings`
  - `DeploymentManifestSettings`
  - `PublishSettings`
3. Instantiate generator and invoke `.Generate()`

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

### Application Manifest

|Property|Type|Description|Default|
|-|-|-|-|
|`Include`|`IList<string>`|(Inherited)|`["**"]`|
|`Exclude`|`IList<string>`|(Inherited)|`["**/*.pdb", "**/*.application", "app.publish/**"]`|
|`EntryPoint`|`string`|A relative path to the entry point assembly.|(see below)|
|`IconFile`|`string`|A relative path to the application icon.|(see below)|


#### EntryPoint detection precedence.
1. The first `.exe` file that has `.exe.manifest`.
2. The first `.exe` file

#### IconFile detection precedence.
1. `{EntryPoint}.ico`
2. The first `.ico` file

### Deployment Manifest

|Property|Type|Description|Default|
|-|-|-|-|
|`Include`|`IList<string>`|(Inherited)|`["**/*.manifest"]`|
|`Exclude`|`IList<string>`|(Inherited)|`[]`|
|`ApplicationManifest`|`string`|A relative path to the apllication manifest|The first `.exe.manifest` file|
|`ApplicationName`|`string`|The identifier of the ClickOnce application.|Use the application manifest value.|
|`Publisher`|`string`|The name of the publisher.||
|`SuiteName`|`string`|The name of the suite.||
|`Product`|`string`|The name of the product. Will be used by the shortcut.||
|`SupportUrl`|`string`|A URL for the support.||
|`ErrorReportUrl`|`string`|A URL for the error reporting.||
|`Install`|`bool`|A value indicating whether the application can be used in offline or not.|`false`|
|`CreateDesktopShortcut`|`bool`|A value indicating whether the application shortcut will be created on the user's Desktop or not .|`false`|
|`CompatibleFrameworks`|`IList<CompatibleFramework>`|List of the compatible frameworks|Detected by `.exe.config`'s `<startup>` element.|

## TBD

- Update configurations
- PermissionSet
- File hash
- Signing manifest

## License

MIT