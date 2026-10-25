# Building for Windows ARM64

This fork was built and tested on Windows 11 ARM64 with .NET SDK 10.0.402 and .NET runtime 10.0.13. The app targets `net10.0-windows10.0.19041.0` and uses the `win-arm64` runtime identifier for a native ARM64 executable.

## Requirements

- Windows 10 or later.
- .NET 10 SDK.
- For the framework-dependent build below, the target machine needs the .NET 10 Windows Desktop Runtime for ARM64.

The verified build used a Windows ARM64 machine. The ARM64-specific build step is selecting `win-arm64`; no custom compiler or native-code build step was needed. `dotnet publish` selects the target platform by runtime identifier and supports framework-dependent and self-contained deployment modes; see Microsoft's [deployment guide](https://learn.microsoft.com/en-us/dotnet/core/deploying/) and [self-contained deployment guide](https://learn.microsoft.com/en-us/dotnet/core/versions/selection).

## Restore, test, and publish

Run these commands from the repository root in PowerShell:

```powershell
dotnet restore .\AmethystWindowsTests\AmethystWindowsTests.csproj
dotnet test .\AmethystWindowsTests\AmethystWindowsTests.csproj --configuration Release -p:Platform=AnyCPU --no-restore
dotnet publish .\AmethystWindows\AmethystWindows.csproj --configuration Release --runtime win-arm64 --self-contained false -p:Platform=AnyCPU --output .\artifacts\win-arm64
```

The published files are placed in `artifacts\win-arm64`. To make a self-contained build that includes the .NET runtime, change `--self-contained false` to `--self-contained true`; that package is larger.

Verify that the executable targets ARM64:

```powershell
$exe = '.\artifacts\win-arm64\AmethystWindows.exe'
$bytes = [System.IO.File]::ReadAllBytes($exe)
$peOffset = [BitConverter]::ToInt32($bytes, 0x3c)
$machine = [BitConverter]::ToUInt16($bytes, $peOffset + 4)
if ($machine -ne 0xAA64) { throw ('Expected ARM64 PE machine 0xAA64, found 0x{0:X4}' -f $machine) }
'ARM64 executable verified.'
```

## Layout modes

The default `Alt+Shift+Space` shortcut cycles clockwise through Full Screen, Tall, Three Columns, and 2 × 2 Grid. `Alt+Shift+Win+Space` cycles in reverse. The layout changes only on the monitor under the mouse pointer, and each change displays a brief on-screen label. Entering 2 × 2 Grid resets all dividers to even splits. The grid's vertical divider can be resized independently in each row, and its horizontal divider can be resized independently in each column. Layout geometry, slot stacking, divider calculations, and four-mode cycling are covered by the test project.

## Windows 11 virtual desktop dependency

The app uses `Slions.VirtualDesktop` 6.9.2. The upstream `VirtualDesktop` 5.0.5 dependency failed to resolve `IApplicationView` on Windows 11 build 26100, so this fork uses the compatible replacement. Keep this dependency change when building for ARM64 and Windows 11.

## Installer project

The solution also contains a legacy Visual Studio Installer `.vdproj`. The command-line steps above build and test the app project directly; they do not build an MSI. MSI creation requires Visual Studio with the Installer Projects extension, and an ARM64 MSI was not verified as part of this build.

## Signing

Keep private SSH keys outside the repository. Configure Git's SSH signing format and signing key before committing, enable signed commits and tags, and verify them with `git verify-commit` and `git verify-tag` before publishing. The published build in this fork is distributed from a signed Git commit and signed annotated tag.
