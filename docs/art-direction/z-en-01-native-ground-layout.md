# Z-EN-01 Native Ground Layout

The first native ground proof uses `tile_grass_path_5x1_v01.png`, a 160x32 atlas of five 32x32 tiles:

| Index | Role |
| ---: | --- |
| 0 | Grass base |
| 1 | Path interior |
| 2 | Grass/path edge |
| 3 | Inside corner |
| 4 | Outside corner |

`EnvironmentTileLayer` is an isolated proof layer. It places a 20x11 grass field and a two-row path with explicit edge tiles. It is not yet enabled in the production park because the current 640x360 background still contains baked road pixels.

For a comparison build, set `ZEST_NATIVE_GROUND=1`. The default production path remains unchanged when the variable is absent.

Acceptance gate before replacing the baked background:

- every path transition uses a discrete atlas tile;
- no fractional scale or filtering is introduced;
- stand and customer contact points remain on the same world coordinates;
- the complete 640x360 composition is readable at 3x integer scale;
- the old monolithic background is removed only after a visual comparison capture.
