# TeaCurses

Rift of the NecroDancer BepInEx 5 plugin for **custom game modes (curses)** with a scrollable in-game overlay.

Plugin GUID: `rotn.dimethyltea.TeaCurses` (config: `BepInEx/config/rotn.dimethyltea.TeaCurses.cfg`).

## Controls

| Key | Action |
|-----|--------|
| `=` | Open / close curse overlay on stock or custom track select (remappable: `BepInEx/config` → `Overlay.ToggleKey`) |
| ↑ / ↓ (or W / S) | Move highlight (wraps at ends) |
| Page Up / Page Down | Jump by one visible page (clamps at ends) |
| Home / End | Jump to first / last curse |
| Enter / Space | Toggle highlighted curse (multi-select) |
| ← / → (or A / D) | Step intensity when adjustable (wraps at ends) |

Intensity shows under the list when the highlighted curse has a range.

## v1 curses

- **Alternating Hands** — successive presses must alternate primary/alternate bindings
- **Mirror Controls** (intensity toggle 0/1) — swaps Left↔Right; intensity 1 also swaps Up↔Down
- **One Hand** (intensity toggle 0/1) — only Primary (0) or Alternate (1) bindings register; wrong-side inputs do nothing (no miss/lockout). Mutually exclusive with Alternating Hands.
- **Blink** (intensity 1–10) — each monster gets a random visibility duty cycle; higher intensity allows sparser visibility (down to 1-in-10). Hide is visual only — you still hit them.
- **Afterimage** (intensity 1–10) — real monster sprite always hidden; faded beat-trail ghosts at prior positions (never current cell), tinted by on-beat / half-beat / other spawn phase.
- **Smooth Beats** (intensity 1–10) — blends stock beat-curve motion and linear smooth; higher intensity = more harmonics + faster oscillation.
- **Upwards Rift** — field tiles invert so monsters approach from the bottom to a top action row (sprites stay upright)
- **Sideways Rift** — far approach from side walls with a right-angle turn into the last two top-down rows (sprites stay upright)
- **AlltheWays Rift** (intensity 1–9) — **1** diagonal zigzag, **2** perimeter, **3** original field corkscrew, **4** funnel, **5** serpentine, **6** switchback, **7** crossroads, **8** orbit, **9** three galaxy spiral arms from a shared center (split outward; action tiles at arm tips). Hits L/↑/R. Combines with Sideways/Upwards.
- **Vanishing Point** (intensity 1–10) — enemies fade as they near the action row. I≥6: invisible from 1 beat before through the hit. I<6: action row stays faintly visible. I<3: action row and the row before stay faintly visible. Higher intensity starts the fade farther away. Exclusive with Afterimage; stacks with Blink.
- **Armored** — stock 1-HP monsters take two hits (already multi-hit and health items unchanged). Missing hurt clips get a short white flash / scale punch.
- **Trappist** (intensity 1–10) — remixes chart traps, morphs every beat (each cluster cell independently), and grows a **1×1→3×3** cluster with mixed-type duplicates + spawn burst for short-lived traps. Soft Mystery-cloak at I≥6.
- **Half Window** (intensity toggle 0/1) — 0 = no late (early-only); 1 = no early (late-only). Removed half mistimes like stock out-of-window.
- **Cryptid** (unfair — red when off; intensity **1–3**) — field enemies become shuffled Runic/Cuneiform glyphs each chart. **1** = found Unicode only; **2** = procedural only; **3** = mix. First sighting of a type keeps real art with a superscript glyph tell above the head (learn it on the walk in); later instances are glyph-only. Health items / portraits unchanged. Exclusive with Afterimage + Vanishing Point; stacks with Blink.
- **Edge Rocker** (blocks leaderboard) — input **releases** also count as hit attempts (full mistime/errant). Press and release in the same window are two attempts.
- **Imperfect Rifts** (intensity 1–10) — non-Perfect hits raise a chaos meter that drifts/shakes field tiles; Perfects repair. Resets each chart. Stacks additively on Upwards/Sideways/AlltheWays.

## Build from source

Requires a Rift install with **BepInEx 5**, **[Rift of the NecroManager](https://github.com/96-LB/RiftOfTheNecroManager)** in `BepInEx/plugins`, and the [.NET SDK](https://dotnet.microsoft.com/download) (`dotnet` on your PATH).


1. In `TeaCurses.csproj`, set `$(GameManaged)` to your game’s `…/RiftOfTheNecroDancer_Data/Managed` folder (the default path is my Steam install). You can also pass it on the command line: `-p:GameManaged="D:\path\to\Managed"`.
2. Put a publicized `Assembly-CSharp` at `lib/RiftReadable.dll` (compile-time only; the game still loads its own Managed assemblies).

   ```bash
   dotnet tool install -g BepInEx.AssemblyPublicizer.Cli
   assembly-publicizer "<GameManaged>/Assembly-CSharp.dll"
   mkdir -p lib
   cp "<GameManaged>/Assembly-CSharp-publicized.dll" lib/RiftReadable.dll
   ```

3. Point `$(GamePlugins)` at your `BepInEx/plugins` folder if needed (default is my Steam install) so the build can reference `RiftOfTheNecroManager.dll` (`Private=false` — not copied beside this mod).
4. Build and install:

```bash
dotnet build -c Release
# then copy bin/Release/netstandard2.1/TeaCurses.dll → <game>/BepInEx/plugins/
```

Optional: `dotnet test TeaCurses.Tests/TeaCurses.Tests.csproj`
