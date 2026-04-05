# AGENTS.md — AI Context for NostalgiaMenu

This file gives AI coding assistants full context to work on this project safely and effectively.

---

## What This Project Is

NostalgiaMenu is a **Windows kiosk application** for arcade cabinets running Nostalgia and BeatStream games. It displays a fullscreen grid of game tiles read from a config file. The player taps a tile to launch that game, or waits for a countdown to auto-launch a default game.

It is a **C# rewrite** of an original VB.NET project ([camprevail/NostalgiaMenu](https://github.com/camprevail/NostalgiaMenu)).

---

## Hard Constraints — Never Violate These

1. **Target framework is .NET Framework 4.6.2.** Do not upgrade to .NET 5/6/7/8 or .NET Core. The app runs on Windows 7 SP1 embedded machines with no internet access.
2. **Zero external dependencies.** No NuGet packages. No third-party DLLs. Only BCL `System.*` assemblies and WPF framework assemblies (`PresentationCore`, `PresentationFramework`, `WindowsBase`, `System.Xaml`).
3. **No internet calls.** The machine is offline at runtime. Do not add any HTTP clients, telemetry, update checks, or remote resource loading.
4. **Windows-only.** This is a WPF application. Do not introduce cross-platform abstractions.
5. **C# 6 syntax maximum** (supported by VS2017 with .NET 4.6.2). Avoid C# 7+ features (e.g. pattern matching `switch`, `ValueTuple`, `out var`, default interface methods) unless you have confirmed the build environment supports them.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Language | C# 6 |
| UI Framework | WPF (Windows Presentation Foundation) |
| Target Runtime | .NET Framework 4.6.2 |
| Config Format | INI (custom parser, no library) |
| Build System | MSBuild via `.csproj` (old-style, non-SDK format) |
| IDE | Visual Studio 2017+ |

---

## Project Structure

```
NostalgiaMenu/
├── NostalgiaMenu.sln
├── README.md
├── AGENTS.md
├── .gitignore
└── NostalgiaMenu/
    ├── NostalgiaMenu.csproj       ← MSBuild project, .NET 4.6.2, WinExe
    ├── App.xaml                   ← Global color/brush resources
    ├── App.xaml.cs                ← Application entry point (empty shell)
    ├── MainWindow.xaml            ← Fullscreen WPF layout
    ├── MainWindow.xaml.cs         ← All runtime logic (tile building, timer, launch)
    ├── Models/
    │   └── GameEntry.cs           ← Plain data class for one game entry
    ├── Parsers/
    │   └── IniParser.cs           ← Custom INI parser, no dependencies
    ├── Controls/
    │   └── CountdownRing.cs       ← Custom FrameworkElement arc ring
    └── Properties/
        └── AssemblyInfo.cs
```

---

## Architecture Overview

### Data Flow

```
App start
  └── MainWindow_Loaded()
        ├── IniParser.Parse("games.ini")     → Dictionary<section, Dictionary<key, value>>
        ├── LoadGames()                       → List<GameEntry>
        ├── BuildTile(entry) × N             → Border UIElements added to ItemsControl
        └── if DEFAULT GAME exists:
              StartCountdown()               → DispatcherTimer ticks every 1s
              Timer_Tick()                   → updates SecondsText + CountdownRingControl.RemainingSeconds
              at 0s → LaunchGame(defaultGame)

User interaction:
  tile MouseEnter/TouchDown → AnimateTileHover / AnimateTilePress (ScaleTransform + DropShadowEffect)
  tile MouseUp/TouchUp      → LaunchGame(entry) → Process.Start() → App.Shutdown()
  Escape key                → App.Shutdown()
```

### Key Design Decisions

**Tiles built entirely in code-behind** (`BuildTile()` in `MainWindow.xaml.cs`), not with a DataTemplate. This is intentional — each tile needs a unique `DropShadowEffect` instance to animate independently. A shared DataTemplate style would share the effect instance across tiles.

**`FrameworkElement` for the countdown ring** (`CountdownRing.cs`) rather than a `Path` + `ArcSegment` + `IValueConverter`. The custom element exposes `RemainingSeconds` as a `DependencyProperty` with `AffectsRender`, so the `DispatcherTimer` only needs to write one property and the arc redraws automatically.

**`WrapPanel`** is used inside the `ItemsControl` so tiles wrap to additional rows on lower-resolution screens. If you want a single horizontally-scrolling row (original behavior), swap `WrapPanel` with `StackPanel Orientation="Horizontal"`.

**`UseShellExecute = true`** in `ProcessStartInfo` is required because `.bat` files need the Windows shell to execute. Setting it to `false` would silently fail.

**No `Window.AllowsTransparency`** — transparency forces software rendering on all WPF layers. The opaque dark background achieves the same look without the performance cost.

---

## File-by-File Reference

### `IniParser.cs`
- Static class, single `Parse(filePath)` method.
- Returns `Dictionary<string, Dictionary<string, string>>` with `OrdinalIgnoreCase` comparers at both levels.
- Skips blank lines and lines starting with `;` or `#`.
- Inline comment stripping is conservative: only strips `;`/`#` preceded by whitespace, so Windows paths are not accidentally truncated.
- Returns an empty dictionary (does not throw) if the file doesn't exist.

### `GameEntry.cs`
Plain data class. Fields:
- `SectionName` — raw INI section name
- `DisplayName` — from `name=` key, or falls back to `SectionName`
- `LauncherPath` — from `launcher=` key
- `ImagePath` — from `image=` key, nullable
- `Color` — from `color=` key (`"gold"` or `"blue"`), nullable
- `IsDefault` — true if `SectionName` equals `"DEFAULT GAME"` (case-insensitive)

### `CountdownRing.cs`
Custom `FrameworkElement`. Dependency properties:
- `TotalSeconds` (default 60)
- `RemainingSeconds` (default 60) — write this from the timer tick
- `RingColor` (default Gold)
- `TrackColor` (default `#2A2A2A`)
- `StrokeThickness` (default 8)

`OnRender` draws a full-circle track, then a clockwise arc from −90° covering `RemainingSeconds / TotalSeconds` of the circle. Handles edge cases: fraction ≥ 1 draws a full circle; fraction ≤ 0 skips the arc.

### `App.xaml`
Defines global `SolidColorBrush` resources referenced by key throughout the XAML:
- `BackgroundBrush` (`#0D0D0D`)
- `TileBorderBrush` (`#2A2A2A`)
- `NeonGoldBrush` (`#FFD700`)
- `NeonBlueBrush` (`#00BFFF`)
- `TextBrush` (`#EEEEEE`)
- `FooterBrush` (`#111111`)

### `MainWindow.xaml`
Three-row `Grid`:
- Row 0 (90px): Header `TextBlock` with static gold `DropShadowEffect`
- Row 1 (`*`): `ScrollViewer` (horizontal pan) → `ItemsControl` with `WrapPanel`
- Row 2 (130px): Countdown footer (`CountdownPanel`, `Visibility="Collapsed"` until a default game is found)

`WindowStyle="None"` + `WindowState="Maximized"` = borderless fullscreen. `KeyDown` is wired to `Window_KeyDown` for Escape handling.

### `MainWindow.xaml.cs`
All runtime logic lives here:
- `MainWindow_Loaded` — orchestrates startup
- `LoadGames` — maps INI dictionary to `List<GameEntry>`
- `BuildTile` — constructs a `Border` element with image/fallback layers, name strip, default-dot, and event handlers
- `BuildFallbackBackground` — gradient + text for tiles with no cover art
- `AnimateTileHover` / `AnimateTilePress` / `EnsureScaleTransform` — animation helpers
- `StartCountdown` / `Timer_Tick` — countdown logic
- `LaunchGame` — `Process.Start` + `App.Shutdown`
- `Window_KeyDown` — Escape to exit
- `CreateTemplateIni` — writes a commented template on first run

---

## games.ini Reference

```ini
[DEFAULT GAME]          ← special: triggers auto-launch countdown
name     = Nostalgia    ← optional display label (defaults to section name)
launcher = C:\Games\Nostalgia\start.bat   ← required
image    = C:\Games\Nostalgia\cover.png   ← optional cover art (PNG/JPG)
color    = gold         ← optional: gold | blue (tile accent color)

[BeatStream]
launcher = C:\Games\BeatStream\start.bat
image    = C:\Games\BeatStream\cover.png
color    = blue
```

`games.ini` must live in the same directory as `NostalgiaMenu.exe`. If missing, a template is auto-generated on startup and the app exits.

---

## Common Maintenance Tasks

### Add a new config key per game
1. Add the key to the INI template in `CreateTemplateIni()` with a comment.
2. Read it in `LoadGames()` and store it on `GameEntry`.
3. Use it in `BuildTile()`.

### Change tile size
Tile dimensions are hardcoded in `BuildTile()`: `Width = 220`, `Height = 220`, `Margin = new Thickness(12)`. Change those values. No other layout changes are needed — `WrapPanel` reflows automatically.

### Change the countdown duration
Change the `CountdownTotal` constant at the top of `MainWindow.xaml.cs`.

### Change the auto-launch section name
The string `"DEFAULT GAME"` is matched in `LoadGames()`. Update the comparison string there and in `CreateTemplateIni()`.

### Add a new accent color option
In `BuildTile()`, the color selection logic is:
```csharp
bool useGold = entry.IsDefault ||
               (entry.Color != null &&
                entry.Color.IndexOf("gold", StringComparison.OrdinalIgnoreCase) >= 0);
var accentColor = useGold ? Color.FromRgb(0xFF, 0xD7, 0x00)
                          : Color.FromRgb(0x00, 0xBF, 0xFF);
```
Extend this if/else chain for new colors. Update `CreateTemplateIni()` and the README to document the new option.

### Switch to single-row horizontal scroll
In `MainWindow.xaml`, inside `ItemsControl.ItemsPanel`, replace:
```xml
<WrapPanel Orientation="Horizontal" />
```
with:
```xml
<StackPanel Orientation="Horizontal" />
```

---

## What to Avoid

- Do not add NuGet packages. If you need a utility (JSON parsing, logging, etc.), implement it inline or use a built-in BCL class.
- Do not use `async`/`await` for UI timer work — `DispatcherTimer` already runs on the UI thread. Keep it simple.
- Do not set `Window.AllowsTransparency = true` — it forces software rendering.
- Do not set `UseShellExecute = false` for `.bat` launchers — it will fail silently.
- Do not share a single `DropShadowEffect` instance across multiple tiles — WPF effects are not thread-safe when animated simultaneously. `BuildTile()` creates a new instance per tile.
