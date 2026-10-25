# Archived

The upstream project was archived in May 2023. This fork adds a native Windows ARM64 build and keeps the tiling behavior to the two Amethyst layouts below.

See [BUILDING.md](BUILDING.md) for the ARM64 build steps and runtime notes.

# Amethyst Windows v2

[![Gitter](https://badges.gitter.im/glsorre/amethystwindows.svg)](https://gitter.im/glsorre/amethystwindows?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
[![GitHub labels](https://img.shields.io/github/labels/glsorre/amethystwindows/help%20wanted)](https://github.com/glsorre/amethystwindows/labels/help%20wanted)

The **dynamic / automatic tiling window manager** for **windows 10/11** along the lines of [ianyh/Amethyst](https://github.com/ianyh/Amethyst).

A very quick screencast of basic functions is [available](https://www.youtube.com/embed/AWN_KehMzHc).

## Features

- integrated with **windows 10/11 virtual desktops**
- fully **customizable shortcuts**
- supports **multi-monitor** set-ups
- **automatic updates**
- **open source**
- includes a **native Windows ARM64** build

# Documentation

## Default Keyboard Shortcuts

Amethyst Windows uses two modifiers.

| Shortcut                  | Description                          |
|---------------------------|--------------------------------------|
| `alt + shift`             | mod1                                 |
| `alt + shift + win`       | mod2                                 |


The keyboard shortcuts configured are:

| Shortcut                  | Description                                   |
|---------------------------|-----------------------------------------------|
| `mod1 + space`            | Toggle between Tall and Monocle               |
| `mod2 + space`            | Toggle between Tall and Monocle               |
| `mod1 + enter`            | Swap focused window to main window            |
| `mod1 + H`                | Swap focused window counterclockwise          |
| `mod1 + L`                | Swap focused window clockwise                 |
| `mod1 + J`                | Move focus to previous window                 |
| `mod1 + K`                | Move focus to next window                     |
| `mod1 + P`                | Move focus to previous monitor                |
| `mod1 + N`                | Move focus to next monitor                    |
| `mod2 + L`                | Expand main pane                              |
| `mod2 + H`                | Shrink main pane                              |
| `mod2 + K`                | Move window to next monitor                   |
| `mod2 + J`                | Move window to previous monitor               |
| `mod1 + Z`                | Force windows to be revalutated               |
| `mod2 + left`             | Throw focused window to virtualdesktop left   |
| `mod2 + right`            | Throw focused window to virtualdesktop right  |
| `mod2 + 1`                | Throw focused window to virtualdesktop 1      |
| `mod2 + 2`                | Throw focused window to virtualdesktop 2      |
| `mod2 + 3`                | Throw focused window to virtualdesktop 3      |
| `mod2 + 4`                | Throw focused window to virtualdesktop 4      |
| `mod2 + 5`                | Throw focused window to virtualdesktop 5      |

## Layouts

### Tall
Tall has two side-by-side slots. New windows alternate between the slots and cover the older window in their slot. Drag either side of the shared divider to resize it; both panes follow the new split. When only one managed window remains, it fills the work area.

### Monocle
Monocle gives every managed window the same full-screen slot. The foreground window appears on top. Moving or resizing a window snaps it back into the full-screen slot.

# About

## Contact

Please contact me trough [twitter](https://twitter.com/glsorre) or [gitter](https://gitter.im/glsorre/amethystwindows)

## License

This software is released with the [MIT license](https://github.com/glsorre/amethystwindows/blob/master/LICENSE).

## Contributing

Feel free to fork master and open a PR.

I will add issues where I think you could start from and label them as help wanted.

## Credits

A big credit goes to [ianyh/Amethyst](https://ianyh.com/amethyst/). This is simply its port on the windows 10 operating system.

