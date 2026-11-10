# Windows build setup

The upstream baseline is Lite 1.9.2 at `94adfd9`. Initialize its pinned ADLX
submodule and run the existing tests from the repository root.

```powershell
git submodule update --init --recursive
dotnet run --project Tests/Shared.Tests
```

## Toolchain

| Component | Project requirement |
| --- | --- |
| Visual Studio 2022 MSBuild | UWP XAML, .NET Native and Desktop Bridge packaging targets |
| Windows SDK | 10.0.26100.0 for the widget and package projects. Shared also resolves this SDK's `UnionMetadata\10.0.26100.0\Windows.winmd`. |
| .NET Framework | 4.8 developer tools and targeting pack for the helper |
| .NET SDK | 10.0 for tests targeting `net10.0-windows10.0.26100.0` |
| Widget NuGet packages | Xbox Game Bar 7.3.2506120, UWP 6.2.9, Microsoft.UI.Xaml 2.8.7 |

On the development PC, VS Build Tools 17.14.37827.9, Windows SDK
10.0.26100.0 and .NET SDK 10.0.403 were available. The unchanged baseline
passed all 55 Shared tests. The package build stopped at MSB4019 for
`Microsoft.DesktopBridge.props`. A standalone helper build stopped at
MSB3644 for the .NET Framework 4.8 reference assemblies.

Add the WinUI application development build tools workload and .NET Framework
4.8 targeting pack to the existing Build Tools instance. This requires an
elevated PowerShell. The component IDs come from the
[Visual Studio 2022 Build Tools catalog](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-build-tools?view=vs-2022).

```powershell
$vsRoot = "${env:ProgramFiles(x86)}\Microsoft Visual Studio"
$vsInstall = & "$vsRoot\Installer\vswhere.exe" -latest -products Microsoft.VisualStudio.Product.BuildTools -property installationPath
& "$vsRoot\Installer\setup.exe" modify --installPath "$vsInstall" `
    --add Microsoft.VisualStudio.Workload.UniversalBuildTools `
    --add Microsoft.Net.Component.4.8.TargetingPack --passive --norestart
```

## Baseline package

Run on a fresh checkout or before generating other package outputs.

```powershell
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -products '*' -find 'MSBuild\Current\Bin\amd64\MSBuild.exe' |
    Select-Object -First 1
& $msbuild XboxGamingBarPackage/XboxGamingBarPackage.wapproj /restore /t:Build `
    /p:Configuration=Release /p:Platform=x64 /p:AppxBundlePlatforms=x64 `
    /p:UapAppxPackageBuildMode=SideloadOnly /p:AppxBundle=Always `
    /p:AppxPackageSigningEnabled=false /p:AppxSymbolPackageEnabled=false
```

For a signed developer release, the existing `Build-Release.ps1` expects a
private signing key in the current user's certificate store with the manifest
publisher `CN=dg`. Follow its certificate instructions and pass the intended
thumbprint explicitly. The package identity is
`PlayandBuildCustom.10365195AA1EC`. Keep private keys and PFX files outside
the checkout. The GUI installer uses `ps2exe`. Verify the build output and
signature before deployment. The current release script selects the newest
bundle under `AppPackages` even when MSBuild returns an error, so an old
bundle can mask a build failure.
