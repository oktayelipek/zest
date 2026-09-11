# Architecture

`Zest.Domain` owns the authoritative runtime state and simulation rules. Its
types are plain C# objects and must not inherit from Godot types.

The Godot project under `game/` is the presentation and application shell. It
may read domain state and issue commands through public domain APIs, but it must
never mutate cash, inventory, customers, orders, or other domain data directly.

State-changing property setters and collection mutation APIs therefore remain
internal to `Zest.Domain`. Later tasks will add explicit command/application
boundaries without weakening this ownership rule.

## Simulation time

`SimulationClock` converts elapsed wall-clock time into ordered authoritative
fixed steps. Pause and speed are domain commands; render frames only contribute
elapsed duration. This keeps simulation results independent of render FPS and
allows the same clock to run from Godot or the headless `Zest.SimRunner`.

## Deterministic randomness

Random decisions use named, isolated streams derived from
`Hash(rootSeed, day, stream, entity)`. Stream names are stable domain API and
must not be shared by unrelated systems. The seed descriptor is suitable for
telemetry/debug traces, while regression vectors protect the hashing and
SplitMix64 algorithms from accidental changes.

## Data-driven content

`Zest.Config` loads the versioned manifest and all balancing/content definitions
from `config/`. Stable IDs form references between files. Startup fails with a
collected validation report when an ID, range, reference, or unit is invalid.
The manifest version is copied into each initial `GameState` so simulation
fixtures identify the exact content revision they used.

## World opportunities

`WorldGenerator` combines calendar context, location traffic curves, weather,
temperature, and hourly segment weights into deterministic passerby
opportunities. It cannot create customers or mutate sales; those decisions are
owned by later domain systems. Weather and traffic use separate named RNG
streams, and all pressure modifiers originate in config.

## Customer choice

`CustomerGenerator` turns an opportunity into a registered `CustomerState` and
evaluates notice, interest, product utility, price penalty, and a softmax
outside option. Product purchase is the only positive outcome; every
non-purchase carries a `LostSaleReason` for telemetry. Customer choice uses its
own named RNG stream, so cosmetic or world draws cannot perturb decisions.

## Products and inventory

Recipes are immutable numbered versions. Product taste dimensions, COGS, and
workload are derived when a version is created, while historical orders retain
the exact version ID they accepted. Inventory truth is stored as raw lots.
Orders reserve stock atomically, then either consume or release that reservation
exactly once; availability accounts for active reservations to prevent oversell.

## Economy ledger

All financial changes are immutable, append-only ledger facts expressed in minor
currency units. Serving an order atomically posts its sale and COGS once. Waste,
labor, and rent are explicit costs; cash impact is recorded separately from profit
impact so revenue, profit, and cash remain distinct. Cached cash must reconcile to
opening cash plus every cash-affecting ledger entry.

## Order work scheduling

Accepted orders expand immutable recipe steps into authoritative work tasks.
Tasks enter a deterministic FIFO queue and acquire station, equipment, and staff
capacity as one unit, preventing partial allocation and double-booking. Only the
next step of an order is eligible; the order becomes ready after every step is
complete and cannot be served earlier. Expected and actual waits remain separate
facts for live UI and reporting.

## Godot application shell

The Godot project renders three deliberately separate layers: quiet world context,
high-contrast operational controls, and editorial insight/reporting. It observes
read-only snapshots and submits `IDayCommand` values through `DayCommandProcessor`;
it never writes authoritative properties or collections. Morning Brief, Live, and
Daily Report therefore use the same command path as the headless runner, making a
command sequence reproducible without Godot.

## Queue abandonment

Every accepted order carries an immutable patience profile. Cumulative abandonment
probability rises smoothly with elapsed wait and grows faster when actual wait
exceeds the original expectation. A named per-order RNG threshold makes repeated
evaluations deterministic and monotonic. Abandonment atomically cancels unfinished
work, frees operational resources, releases the active inventory reservation once,
and appends a `QueueAbandonment` loss event. Daily and telemetry reports count this
separately from pre-purchase choice losses.
