# NostalgiaMenu

A touch-screen game launcher for Nostalgia / BeatStream arcade cabinets. Displays a full-screen tile grid of configured games and launches the selected one. A configurable default game auto-launches after a countdown if no selection is made.

Built with **C# / WPF / .NET Framework 4.6.2** — no external dependencies, runs on Windows 7 SP1+.

---

## Features

- Fullscreen borderless kiosk UI with dark arcade theme
- Cover art images per game tile (falls back to styled text tile if no image)
- Neon glow and scale animations on hover, touch, and press
- Animated arc countdown ring for auto-launch
- Horizontal touch-scrolling if more tiles than fit on screen
- Escape key exits the app

---

## Requirements

- Windows 7 SP1 or later
- [.NET Framework 4.6.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net462) (included in Windows 10+; available as a free update for Win7 SP1)
- No internet connection required at runtime

---

## Building

Open `NostalgiaMenu.sln` in Visual Studio 2017 or later and press **Build**. No NuGet restore is needed — there are zero external dependencies.

The output binary will be at `NostalgiaMenu/bin/Release/NostalgiaMenu.exe`.

---

## Configuration

On first launch, if no `games.ini` is found next to the `.exe`, a commented template is created automatically and the app exits. Edit that file to add your games, then relaunch.

### games.ini format

```ini
; Each [Section] defines one game tile.
;
; launcher = path to start.bat  (required)
; image    = path to cover art  (optional, PNG/JPG)
; color    = gold | blue        (optional tile accent color)
; name     = Display Name       (optional label override)
;
; [DEFAULT GAME] auto-launches after 60 seconds of inactivity.

[DEFAULT GAME]
name     = Nostalgia
launcher = C:\Games\Nostalgia\start.bat
image    = C:\Games\Nostalgia\cover.png
color    = gold

[BeatStream]
launcher = C:\Games\BeatStream\start.bat
image    = C:\Games\BeatStream\cover.png
color    = blue
```

### Keys

| Key | Required | Description |
|---|---|---|
| `launcher` | Yes | Path to the game's `start.bat` or executable |
| `image` | No | Path to cover art image (PNG or JPG). Recommended size: 220×220 px |
| `color` | No | Tile accent color — `gold` or `blue`. Defaults to blue |
| `name` | No | Display name shown on the tile. Defaults to the section name |

The `[DEFAULT GAME]` section name is special — that tile gets a gold dot indicator and triggers the auto-launch countdown. All other section names are treated as regular game tiles.

---

## Credits

Based on [NostalgiaMenu](https://github.com/camprevail/NostalgiaMenu) by camprevail (original VB.NET / .NET Framework 3.5 version).
