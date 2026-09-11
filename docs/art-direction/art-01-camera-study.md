# ART-01 — World Camera Greybox & Readability Study

## Decision

Use a stylized 2.5D orthographic camera as the default **Business View**. It
keeps the stand, full five-person queue, and both directions of pedestrian flow
legible in one frame without asking the player to manage a free camera.

## Authored framing

| View | Camera offset | Orthographic size | Purpose |
| --- | --- | ---: | --- |
| Business (default) | `(10, 13, 15)` | `13` | Stand, queue, immediate flow |
| Neighborhood | `(15, 17, 19)` | `19` | Entry/exit zones and whole park rhythm |
| Close Observation | `(7, 9, 10)` | `8.5` | Inspect stand and individual decisions |

All views look at world target `(0, 0.7, 0)`. The resulting preferred angle is
approximately 41° downward with a 34° plan rotation. Orthographic projection is
preferred over perspective; if perspective is revisited, begin at 32° vertical
FOV and reject any setting that hides queue depth behind silhouettes.

Pan is optional and bounded to `x ±3`, `z ±2`. Dragging recenters within those
limits; wheel input selects only the three authored zoom levels. There is no
continuous zoom and no camera rotation.

## Spatial read

- The citrus canopy and dark green sign make the stand the primary landmark.
- The queue occupies its own lighter spur path and alternates rust/blue-grey
  silhouettes so length is countable without a chart.
- Queue positions one through five extend away from the stand; positions six
  through eight turn back on a second authored lane. Peak queue pressure stays
  inside the Business View instead of running off-screen.
- Nine moving silhouettes cross the main path; teal and rust ground bars mark
  entry and exit edges.
- Benches and four trees establish park scale without competing with the stand.
- A raised earth-toned plinth makes the authored play space read as a contained
  diorama. Lamps, planters, a notice board, and a waste bin provide neighborhood
  scale cues while remaining outside the stand/queue silhouette corridor.

## UI safe areas

At the 1920×1080 baseline, the high-contrast live HUD owns the top band and day
controls own the bottom strip. The world viewport sits between them. Stand,
queue, and the center section of pedestrian flow never render beneath critical
UI. Camera controls are compact presets above the viewport rather than floating
over gameplay objects.

## Study result

The Business View is the preferred default. Neighborhood View is contextual and
Close Observation is investigative. Queue pressure should be communicated first
by visible bodies and spacing, while HUD counts remain a redundant accessible
signal. **Track guest** targets the first queued customer, switches to Close
Observation, and places a citrus focus marker without rotating the camera. This
establishes the interaction required for future regular/customer following.
This greybox validates composition and hierarchy only; final geometry,
animation, lighting, and materials remain outside ART-01.
