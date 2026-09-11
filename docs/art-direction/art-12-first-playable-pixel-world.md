# ART-12 — First Playable Pixel World Slice

## Result

`game/scenes/art12_playable.tscn` is the focused ART-12 scene. It opens directly
in the live phase while retaining the same authoritative C# simulation and
world-first HUD used by the normal game flow.

The former 3D greybox and the first procedural placeholder have been replaced
by a layered high-density 2D pixel presentation:

- 640×360 world viewport presented at integer 3× scale inside the 1920×1080 UI;
- authored Riverside riverbank, grass, flower, fence, and crossing-path backdrop;
- central yellow Zest stand, queue lane, stock state, and waiting-state label;
- independently layered stand, trees, bench, lamp/sign, and flower planter;
- one autonomous route customer plus simulation-driven queue customers;
- fixed south/west/east/north sheet rows and 8 fps four-frame walking;
- integer actor movement, nearest sampling, and Camera2D without smoothing;
- Y-sorting at the contact point for props, stand, and customers.

## Interaction and readability

- Click the stand to open product/inventory actions.
- Click a waiting customer to inspect or track them.
- Drag the park to pan and use the wheel for the three camera framings.
- Use the bottom hotbar for stand, customers, notebook, and map.
- Time, cash, reputation, speed, and day progress remain in compact edge HUDs;
  the center of the park and queue stay unobstructed.

## Production source and runtime assets

| Source master | Runtime copy |
| --- | --- |
| `art-source/production/final-grid-v02/bg_riverside_640x360_v02.png` | `game/art/production/final-grid-v02/bg_riverside_640x360_v02.png` |
| `art-source/production/final-grid-v02/prop_zest_stand_idle_v02.png` | `game/art/production/final-grid-v02/prop_zest_stand_idle_v02.png` |
| `art-source/production/final-grid-v02/chr_guest_walk_4x4_v02.png` | `game/art/production/final-grid-v02/chr_guest_walk_4x4_v02.png` |
| `art-source/production/final-grid-v02/prop_park_tree_idle_v02.png` | `game/art/production/final-grid-v02/prop_park_tree_idle_v02.png` |
| `art-source/production/final-grid-v02/prop_park_bench_idle_v02.png` | `game/art/production/final-grid-v02/prop_park_bench_idle_v02.png` |
| `art-source/production/final-grid-v02/prop_lamp_sign_idle_v02.png` | `game/art/production/final-grid-v02/prop_lamp_sign_idle_v02.png` |
| `art-source/production/final-grid-v02/prop_flower_planter_idle_v02.png` | `game/art/production/final-grid-v02/prop_flower_planter_idle_v02.png` |
| `art-source/production/final-grid-v02/prop_bush_wind_4x1_v02.png` | `game/art/production/final-grid-v02/prop_bush_wind_4x1_v02.png` |

The stand, individual props, four-direction guest sheet, and four-frame wind
bush retain transparent backgrounds. All runtime assets now match their final
640×360-grid footprint and render at scale 1. Godot imports them as
uncompressed 2D sources with nearest filtering and no mipmaps.

## ART-10 fallback manifest

| ID | Runtime use |
| --- | --- |
| `grass_base` | repeating 16×16 park ground |
| `path_edge_base` | seam-safe path boundary |
| `zest_stand_base` | primary focal object |
| `lemon_crate` | stand-side stock cue |
| `guest_base_walk` | 4×4 directional customer sheet |
| `park_tree_leafy` | rear/front depth framing |
| `park_bench_wood` | west-side seating prop |
| `park_lamp_sign` | east-side wayfinding prop |
| `park_planter_flowers` | warm stand-side accent |

These entries remain declared in `art-source/pipeline.json`, published
byte-for-byte to `game/art/pixel/`, and validated by
`tools/validate-art-pipeline.ps1`. They are technical fallback assets; the
ART-12 shipping composition now uses the production files above.

## Evidence

- Production screenshot: `docs/artifacts/art-12-first-playable-pixel-world.png`
- Direction reference: `docs/artifacts/art-12-concept-reference.png`
- Asset fidelity reference: `docs/artifacts/art-12-asset-reference-v2.png`
- Automated contract: `tools/validate-art12-world-slice.ps1`
- Reproducible final-grid build: `tools/build-art12-final-grid.ps1`

The generated direction and asset references informed focal hierarchy, richer
foliage clusters, striped awning construction, hard-edged shadows, warm park
framing, and HUD-safe negative space. The production background, prop layers,
stand, and character atlas were generated as separate masters so gameplay can
move and sort actors without flattening the whole scene into one illustration.
