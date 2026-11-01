# Example Amethyst Windows settings

These files are a snapshot of the fork maintainer's working settings, including the 21 configured shortcuts, zero window padding and margins, and a resize step of 25.

- `MySettings.json` is the exact saved settings snapshot. Its `DesktopMonitors` entries include the original computer's virtual-desktop identifiers, monitor coordinates, saved layouts, and divider positions. Amethyst ignores entries for virtual desktops it cannot find, so those per-monitor settings may not carry over to another Windows installation.
- `MySettings-portable.json` contains the same other settings and shortcuts with the machine-specific `DesktopMonitors` entries removed. Amethyst creates fresh per-monitor settings and starts them in Two Pane mode. Choose another layout with the configured layout shortcut if desired.

To try either file:

1. Quit Amethyst using **Exit** in its tray menu.
2. Back up your existing `%LOCALAPPDATA%\AmethystWindows\MySettings.json`, if present.
3. Copy the example you want to `%LOCALAPPDATA%\AmethystWindows\MySettings.json`. If using the portable file, rename the copy to `MySettings.json`.
4. Start Amethyst again.

The installer offers the portable example as recommended settings for a new installation. It only copies the file when no settings file exists; it does not replace or update existing settings. You can also use either example manually as described above.
