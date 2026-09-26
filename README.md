# Paint Platformer (CSCI 526 Paired Prototype)

**Logline:** A 2D platformer where the player paints terrain with colors that rewrite its physics (slippery, bouncy, or scorching) to build their own path to the goal. *(Platformer + Terrain Painting)*

**Team:** Yixiao Li (A) · [Teammate Name] (B)

## Scope
- One level, gray-box only (Unity primitives / code-drawn shapes). No art polish.
- **No external assets** of any kind (art, audio, fonts, Asset Store packages).
- Unity **6000.6.0f1** (Unity 6.6). Both teammates must use this exact version.

## Game rules (target behavior)
| Color | Key | Effect | How it helps | How it hurts |
|---|---|---|---|---|
| Blue (Slide) | 1 | ~2x run speed, no braking | Jump farther (~5 tiles vs ~2.5) | Slide off ledges |
| Green (Bounce) | 2 | Auto-bounce ~3.5 tiles high | Reach high ledges | Bounce into ceiling spikes |
| Red (Burn) | 3 | Kills on touch, melts adjacent ice | Clears ice walls | Becomes a new hazard |

- Left click paints a plain gray tile (floor, wall, or ceiling). Spikes, ice, and air can't be painted.
- Paint is limited per level. Dying does not refund paint; **R** resets the level and refills paint.
- Level: spike pit (solved by blue), tall wall under ceiling spikes (green), ice wall (red), then the flag.

## Who writes what
All scripts go in `Assets/Scripts/`. Each person commits their own files from their own GitHub account.

| Owner | Area | Suggested scripts |
|---|---|---|
| **A: Yixiao** | Player + color mechanics | `PlayerController.cs` (move, jump), `ColorEffects.cs` (blue / green / red behavior), `Hazard.cs` (spikes, death, respawn) |
| **B: Teammate** | Level + painting + UI | `LevelBuilder.cs` (builds the level layout), `PaintTool.cs` (mouse painting, paint budget, ice melting), `GameUI.cs` (paint left, messages, win screen, R to reset) |

Agree on the shared interface early (for example: a `Tile` component with a `PaintColor` field that `PaintTool` sets and `ColorEffects` reads).

## Workflow
1. `git pull` before you start working.
2. Don't edit the same file (or the same scene) at the same time. Tell your partner before you save `SampleScene`.
3. Small commits with clear messages, e.g. `Add blue slide effect`.

## Build and deploy (WebGL + GitHub Pages)
1. File > Build Profiles > **Web** > Switch Platform.
2. Player Settings > Publishing Settings > **Compression Format: Disabled**.
3. Build into a folder named **`docs`** at the repo root, commit, and push.
4. Repo Settings > Pages > Deploy from branch `main`, folder `/docs`.
5. Test the Pages link in a private window in Chrome and Safari before submitting. Do not submit a Unity Play link.
