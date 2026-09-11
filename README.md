<div align="center">

# 🍋 Project Zest

**A park-side lemonade stand management game built on a deterministic, engine-independent simulation.**

[![Validate](https://github.com/oktayelipek/zest/actions/workflows/validate.yml/badge.svg)](https://github.com/oktayelipek/zest/actions/workflows/validate.yml)
![Godot](https://img.shields.io/badge/Godot-4.x%20.NET-478CBF?logo=godot-engine&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white)
![License](https://img.shields.io/badge/license-TBD-lightgrey)

</div>

---

## What is Zest?

Zest is a small-business simulation game: you run a lemonade stand in a sunny
riverside park, serve a queue of guests, manage stock and price, and watch the
consequences of every decision in a live pixel world and an end-of-day report.

It is built around one core rule: **the simulation is the source of truth, and
the game is a shell on top of it.** All gameplay state and rules live in a
plain C# domain that has no dependency on Godot, so the exact same run can be
replayed in the editor, in a headless runner, or in automated tests.

## Highlights

- 🧠 **Engine-independent simulation** — `Zest.Domain` owns all authoritative
  state; Godot only reads snapshots and submits commands.
- 🎲 **Fully deterministic runs** — named, isolated RNG streams derived from a
  root seed make every day reproducible and testable.
- 📊 **Data-driven content** — products, recipes, customers, world traffic, and
  operations are versioned JSON in `config/`, validated at startup.
- 🕹️ **World-first live interface** — a 640×360 pixel park scaled into a 1080p
  HUD, with contextual panels instead of a dashboard of global KPIs.
- 🧾 **Append-only economy ledger** — sales, COGS, waste, labor, and rent are
  immutable facts, and cached cash always reconciles to the ledger.
- 🖥️ **Headless runner + tests** — the simulation runs and verifies without a
  rendering engine.

## Tech stack

| Layer | Technology |
| --- | --- |
| Presentation / app shell | Godot 4.x (.NET) with C# |
| Simulation / domain | C# on .NET 10 |
| Configuration | JSON manifest + definitions in `config/` |
| Persistence | `Zest.Persistence` (saves, boundaries, preferences) |
| Telemetry | `Zest.Telemetry` (reports, loss events, traces) |
| CI | GitHub Actions (`dotnet test` + `dotnet build`) |

## Repository layout

```
zest/
├─ game/                     Godot 4 .NET presentation/application shell
│  ├─ scenes/                Playable, benchmark, and preview scenes
│  ├─ scripts/               C# presentation nodes (read-only domain access)
│  └─ art/                   Runtime assets + pixel import contract
├─ src/
│  ├─ Zest.Domain/           Authoritative simulation and runtime state
│  ├─ Zest.Config/           Config definitions, loading, validation
│  ├─ Zest.Persistence/      Save/load, day boundaries, user preferences
│  └─ Zest.Telemetry/        Reports, loss events, debug traces
├─ tests/
│  ├─ Zest.Domain.Tests/     Domain unit tests
│  └─ Zest.Config.Tests/     Config and session integration tests
├─ config/                   Data-driven content (manifest, products, world…)
├─ tools/                    Sim runner, asset pipeline, validators, scripts
├─ art-source/               Source masters, concepts, pipeline manifest
└─ docs/                     Architecture, UI, art direction, release notes
```

## Prerequisites

- **.NET 10 SDK**
- **Godot 4.x (.NET)** — required to run the game, not to build or test the domain
- **Windows** for the packaged release (the simulation itself is portable)

## Getting started

### 1. Clone and build

```powershell
git clone https://github.com/oktayelipek/zest.git
cd zest
dotnet build Zest.sln -c Release --nologo
```

### 2. Run the test suite

```powershell
dotnet test Zest.sln --nologo
```

### 3. Play the demo

```powershell
powershell -ExecutionPolicy Bypass -File tools/run-demo.ps1
```

The script locates `godot`/`godot4` on `PATH`. Otherwise set `ZEST_GODOT` or
pass an explicit path:

```powershell
powershell -ExecutionPolicy Bypass -File tools/run-demo.ps1 -GodotPath 'C:\path\to\Godot.exe'
```

Pass `-NativeGround` to preview the opt-in native tile layer without changing
the default production scene.

## Controls

| Action | Input |
| --- | --- |
| Select stand / customer / world | Left click |
| Pan the park | Drag |
| Change camera framing | Mouse wheel |
| Open Stand / Customers | `1` / `2` |
| Open Notebook / Map | `N` / `M` |
| Return to world | `Escape` |
| Pause / resume | `Space` |
| Time speed | On-screen Pause / 1× / 2× / 4× |

## Simulation model

The domain is split into explicit, single-owner systems. Each one exposes a
narrow API and never reaches into another system's state.

- **Simulation clock** — converts elapsed time into ordered fixed steps; pause
  and speed are domain commands, so results never depend on render FPS.
- **Deterministic randomness** — `Hash(rootSeed, day, stream, entity)` produces
  isolated named streams; regression vectors protect the algorithms.
- **World generation** — calendar, traffic curves, weather, and segment weights
  combine into passerby opportunities; it cannot create customers directly.
- **Customer choice** — notice, interest, utility, price penalty, and a softmax
  outside option; every non-purchase carries a telemetry reason.
- **Products & inventory** — recipes are immutable numbered versions; stock is
  stored as raw lots, and reservations prevent overselling.
- **Order scheduling** — recipe steps expand into a deterministic FIFO queue
  that acquires station, equipment, and staff capacity as one unit.
- **Queue abandonment** — patience profiles drive a monotonic, deterministic
  chance to leave; abandonment cancels work and releases reservations exactly once.
- **Economy ledger** — append-only minor-unit facts; cash, revenue, and profit
  stay distinct and reconciled.

See [`docs/architecture.md`](docs/architecture.md) for the full contract.

## Configuration

All content is versioned and validated. The manifest (`config/manifest.json`)
points to stable-ID definitions, and startup fails with a collected report if an
ID, range, reference, or unit is invalid.

```
config/
├─ manifest.json             Content revision (copied into each GameState)
├─ customers/segments.json   Customer segments and behaviour weights
├─ products/ingredients.json Raw materials
├─ products/recipes.json     Versioned recipes and work steps
├─ operations/stations.json  Work stations
├─ operations/equipment.json Equipment capacity
├─ operations/staff.json     Staff roles and capacity
└─ world/                    Locations and weather
```

The manifest version is recorded in each initial `GameState`, so any simulation
fixture identifies the exact content revision it ran against.

## Assets & art pipeline

Zest ships a high-density 2D pixel presentation assembled from separately
authored masters (background, props, stand, characters) so actors can move and
Y-sort without flattening the scene.

- Author on a **16 px source grid**; never pre-scale source PNGs.
- Publish through `tools/Zest.AssetPipeline`; never hand-copy exports.
- Import with nearest filtering, mipmaps off, lossless compression.
- Commit every generated `.import` sidecar.

The contractor-facing contract is in
[`docs/art-direction/art-10-pixel-production-pipeline.md`](docs/art-direction/art-10-pixel-production-pipeline.md),
and the first playable slice is documented in
[`docs/art-direction/art-12-first-playable-pixel-world.md`](docs/art-direction/art-12-first-playable-pixel-world.md).

## Tooling & validation

| Script | Purpose |
| --- | --- |
| `tools/run-demo.ps1` | Launch the playable demo |
| `tools/export-windows.ps1` | Build the Windows release into `dist/Zest.exe` |
| `tools/Zest.SimRunner` | Headless simulation runner |
| `tools/Zest.AssetPipeline` | Asset cleaning and publish pipeline |
| `tools/validate-*.ps1` | Contract validators for UI, art, and features |

Run the full contract suite from the repository root:

```powershell
Get-ChildItem tools/validate-*.ps1 | ForEach-Object { & $_ }
```

## Building a release

```powershell
dotnet test Zest.sln --nologo
dotnet build Zest.sln -c Release --nologo
powershell -ExecutionPolicy Bypass -File tools/export-windows.ps1 -GodotPath 'C:\path\to\Godot.exe'
```

The output is written to `dist/Zest.exe` with its Godot runtime files. Before
distributing, complete one live day in the exported build, save at the next
morning boundary, and load it again. See
[`docs/release/windows-export.md`](docs/release/windows-export.md) for the
full smoke test.

## Continuous integration

Every pull request and every push to `main` runs the
[`Validate`](.github/workflows/validate.yml) workflow on `windows-latest`:

```yaml
dotnet test Zest.sln --nologo
dotnet build Zest.sln -c Release --nologo
```

## Documentation

| Document | Description |
| --- | --- |
| [`docs/architecture.md`](docs/architecture.md) | Simulation ownership and system contracts |
| [`docs/ui/world-first-live-interface.md`](docs/ui/world-first-live-interface.md) | Live HUD, context panels, time controls |
| [`docs/ui/ui-11-hybrid-pixel-components.md`](docs/ui/ui-11-hybrid-pixel-components.md) | Hybrid pixel component system |
| [`docs/art-direction/`](docs/art-direction/) | Visual target, pipeline, and production plan |
| [`docs/release/windows-export.md`](docs/release/windows-export.md) | Release and smoke test procedure |
| [`docs/project-audit-2026-09-10.md`](docs/project-audit-2026-09-10.md) | Project assessment and recovery priorities |

## Roadmap

The current build is a tested simulation foundation with an interactive visual
prototype. The focus is on connecting the systems into one honest end-to-end
business loop.

- [x] Deterministic domain simulation (world, customers, orders, economy)
- [x] Data-driven, validated content pipeline
- [x] World-first pixel presentation and live HUD
- [ ] Full live session wired into the presentation shell
- [ ] One complete day: sellable stock view, closing, and daily report
- [ ] Multi-day loop with save/load and a meaningful upgrade
- [ ] Visual polish: persistent customer identity, animation events, feedback

The recovery order and acceptance criteria live in the
[project assessment](docs/project-audit-2026-09-10.md).

## Contributing

1. Branch from `main`.
2. Keep domain types free of Godot references.
3. Route all state changes through public domain commands — never mutate
   authoritative state from presentation code.
4. Add or update tests, then run `dotnet test Zest.sln --nologo`.
5. Open a pull request; CI must pass before merge.

## License

No license has been added yet. Until one is chosen, all rights are reserved by
the project owner.

<div align="center">

Made with 🍋 and deterministic randomness.

</div>
