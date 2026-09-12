# Zest — art production brief for AI generation

Status: authoritative for new art production, 2026-09-12.
Supersedes production instructions in TECHART-01, ART-02, ART-04, ART-10.
Complements `current-runtime-visual-contract.md` (which describes runtime rules).

## Style anchor

The single canonical visual target is `style-anchor/style-anchor-v1.png`
(1600×900 reference frame). Every new asset must feel consistent with that
image — same warmth, same character silhouette weight, same environmental
density, same signage-carrying "cozy park" tone.

If a prompt output does not read as belonging in that reference, reject it and
re-prompt with a stronger `STYLE_ANCHOR` block or add a specific detail from
this list:

- **Perspective**: top-down 3/4, characters face camera with feet slightly
  angled forward, shadows cast down-and-right.
- **Outlines**: characters carry a chunky near-black outline; props carry a
  lighter warm-brown outline; ground and foliage carry no outline.
- **Environment density**: grass tufts, small flowers, pebbles and stone
  edging populate every empty patch. Never leave a bare green field.
- **Signage voice**: hand-lettered on wood or chalkboard, short and warm.
  Examples in the reference: "Smaller Sips Brighter Days ♥", "GOOD PEOPLE
  BRIGHTER DAYS", signpost "RIVER → / PARK ♥ / KINDER PEOPLE".
- **UX cues in the world**: dashed circle rings on the ground mark queue
  positions; use them wherever the player needs to see where guests stand.
- **Ambient life**: at least one small living thing per scene beyond the
  customers (bird on the bench, duck on the pond, butterfly).
- **Water**: warm dark teal, with lily pads carrying tiny pink flower dots and
  cattail reeds along the edge.
- **Cobble transitions**: the dirt path is bordered by irregular gray cobble
  stones where it meets grass; do not use a hard straight line.
- **Awning**: yellow-and-cream striped with clear ZEST wordmark; a small leaf
  and lemon icon flanks the wordmark.


## 0. Purpose and audience

This document is written to be consumed by an image-generation model or by a
human artist scripting one. Every asset request in this game should be
constructed from these building blocks:

- `PALETTE_LOCK` — the exact 12-color palette
- `STYLE_ANCHOR` — positive style descriptors
- `NEGATIVE_STYLE` — what the model must not produce
- `ASSET_BRIEF` — resolution, view angle, pivot, transparency
- `POSITIVE_PROMPT` and `NEGATIVE_PROMPT` templates
- `DELIVERY` — file format, alpha, background rules

Do not invent new colors, new camera angles, or new stylistic directions.
If a request cannot be satisfied within this brief, escalate a change to
this file rather than deviating in a single asset.

---

## 1. Fixed technical constants

| Key | Value | Notes |
|---|---|---|
| WORLD_LOGICAL_RES | 640 × 360 px | SubViewport canvas ("world pixels") |
| WORLD_RENDER_SCALE | 3× | Godot displays the world at 1920×1080 |
| DAY_CLOCK | 08:00 → 18:00 sim | 10 in-game hours = ~6 real minutes at 1× |
| BASE_TILE | 16 px | Historical source grid; not required for AI layers |
| CAMERA | Orthographic top-down-ish, ~3/4 perspective | Standing figures face camera, feet at bottom |
| TEXTURE_FILTER | Nearest | Runtime enforces nearest sampling |
| MIPMAPS | Off | Runtime enforces mipmaps off |
| COLOR_SPACE | sRGB | No linear/HDR export |
| ALPHA | Premultiplied off; straight PNG alpha | Transparent background required (see per-asset) |

---

## 2. PALETTE_LOCK (12 colors)

Every asset must use only these hex values plus alpha variations. No new hues.
Anti-aliasing that introduces off-palette pixels is not allowed — see
`NEGATIVE_STYLE`.

| Token | Hex | Role |
|---|---|---|
| INK | `#292621` | Line, deep shadow |
| CHARCOAL | `#253333` | HUD panel, night depth |
| PAPER | `#f2ead8` | Warm background surface |
| CREAM | `#fff8e8` | Highlight, text on dark |
| ZEST_YELLOW | `#e8b447` | Brand accent, awning, sunlight |
| LEAF | `#4f7155` | Foliage mid |
| DEEP_LEAF | `#2f4a3c` | Foliage shadow |
| MOSS | `#738c55` | Ground foliage mid |
| RUST | `#a65338` | Warning, apron, planter |
| RIVER | `#557c78` | Water, rain, cool shadow |
| WORLD_WOOD | `#71543a` | Stand structure, benches |
| WORLD_SKIN | `#e7b990` | Human skin base |

Secondary tokens allowed when needed:
`WORLD_GROUND #91a76f`, `WORLD_PATH #d8c9aa`, `WORLD_QUEUE_PATH #c5b18d`,
`WORLD_SKY #d9dfc7`, `WORLD_SHADOW #3f4d3d`, `STONE #a49a85`,
`PRODUCT_BERRY #934f64`, `AMBER #d68e2e`, `SUNLIT_YELLOW #eacb6a`,
`LEAF_HIGHLIGHT #a6b85e`.

Do not add new tokens without updating this file.

---

## 3. STYLE_ANCHOR (positive)

Use these descriptors verbatim in prompts:

```
warm painterly pixel-flavored illustration, low-detail readable silhouettes,
soft diagonal top-left lighting, contact shadow directly beneath the subject,
warm 12-color palette (see palette lock), gentle gouache-style texture,
crisp readable edges, calm quiet neighborhood park atmosphere,
subject centered, feet at bottom edge, transparent background
```

---

## 4. NEGATIVE_STYLE (mandatory)

Include verbatim in every negative prompt:

```
photorealistic, 3d render, glossy plastic, chromatic aberration,
lens flare, depth of field, motion blur, film grain, film scanlines,
outlined comic ink, cell-shaded anime, chibi big-head style,
neon saturation, gradient rainbows, off-palette pastels,
text, watermark, signature, ui elements, hud, arrows, speech bubbles,
extra fingers, extra limbs, warped hands, deformed faces,
white background, checkered background, studio backdrop,
isometric grid, orthographic voxel, MC blocky voxel style,
cluttered background, busy prop stack, dense crowd
```

---

## 5. Delivery format

- PNG, 8-bit RGBA, straight alpha.
- Transparent background unless the asset IS a full 640×360 background.
- No embedded ICC color profile beyond sRGB.
- One subject per file for character/prop layers; atlases only via the pipeline.
- Provide `.import` sidecar via `tools/Zest.AssetPipeline` — never hand-edit.
- File name convention: `<kind>_<subject>_<state>_v<NN>.png`
  - e.g. `chr_customer_commuter_south_walk_a_v01.png`
  - e.g. `prop_zest_stand_awning_rain_idle_v01.png`

---

## 6. Asset classes and per-class briefs

Two source conventions coexist. Pick per class from the table.

| Class | Source convention | Source PNG size | Runtime scale | On-canvas footprint (of 640×360) |
|---|---|---|---|---|
| Full background | Native world-pixel | 640 × 360 | 1.00 | 640 × 360 |
| Environment prop (tree, bench, lamp, planter, bush) | Native world-pixel | 16–64 wide × 16–96 tall | 1.00 | matches source |
| Stand upgrade sprite | Native world-pixel | 170 × 136 (single) or 680 × 136 (4-in-a-row) | 1.00 | 170 × 136 |
| Stand body (Base) | High-res AI layer | 1536 × 1024 | ≈ 0.11 (as set in the scene) | ~170 × 113 |
| Vendor pose | High-res AI layer | 720 × 850 | ≈ 0.041 | ~29 × 35 |
| Customer full-body | High-res AI layer | 800 × 1400 | 0.035 | 28 × 49 |
| UI illustration | Native UI-pixel | any, sized for HUD | 1.00 | as designed |

### 6.1 Full background — 640×360 native world pixel

- `POSITIVE_PROMPT` = `STYLE_ANCHOR` + `park scene, riverside path along bottom third, grass mid, sky top third, morning warm light, no characters, no signage text, no HUD`
- `NEGATIVE_PROMPT` = `NEGATIVE_STYLE`
- Composition: sky ≈ top 90 px, mid-ground grass ≈ 90–260 px, foreground path ≈ 260–360 px.
- Pivot: n/a (fills viewport).

### 6.2 Environment prop — native world pixel

- Author on 16-px source grid (helpful reference, not strict).
- Pivot rule: feet/base at bottom edge, centered horizontally.
- Contact shadow baked into the sprite as a soft polygon at the base.
- Transparent background.
- Positive prompt suffix: `single park prop, no scene, no ground, isolated on transparent background`

### 6.3 Stand upgrade sprite — 170×136 native

- Four upgrade variants share the exact same silhouette and footprint. Only differentiating details change (counter type, cooler size, juicer).
- Deliver either as four separate files or a horizontal 4-in-a-row atlas at 680×136 with variants in this order: `base, better_counter, electric_juicer, bigger_cooler`.
- Pivot: bottom-center of the stand base.
- ZEST_YELLOW awning is the identity mark and must be present on every variant.

### 6.4 Stand body (Base) — 1536×1024 AI layer

- Rendered at approximately 4× target so it downsamples cleanly at ~0.11× runtime scale.
- Vendor is a SEPARATE layer; do not paint the vendor into the stand body.
- Include a slight overhang shadow under the awning and a wooden counter face.
- Pivot: bottom-center of the stand base, aligned so that at runtime scale the base of the stand sits on the world floor line.

### 6.5 Vendor pose — 720×850 AI layer

- Named poses required: `idle`, `prepare`, `pour`, `handoff`, `reach`, `cup_center`.
- All poses must lock to the same skeleton/anchor so swapping one for another does not shift the head or feet in world space.
- View angle: front, slightly angled so hands and cup are readable.
- Apron is `RUST` `#a65338`; sleeves are `CREAM` `#fff8e8`; hair fits the warm palette.
- Cup on `pour` and `handoff` is `PAPER` `#f2ead8` for classic; use `PRODUCT_BERRY` `#934f64` liquid highlight for berry variant if a variant is delivered.

### 6.6 Customer full-body — 800×1400 AI layer

- 4-direction set: `south`, `north`, `west`, plus turn frames `south-west-turn`, `west-south-turn`.
- East is authored by horizontally flipping west at runtime; do NOT deliver east.
- Walk cycles: 2 frames each (`walk-a`, `walk-b`) at least for `south` and `west`; `north` may be `walk-a` only.
- Service poses: `south-receive`, `west-leave`.
- Pivot: feet centered at bottom edge.
- Contact shadow baked as a small ellipse at feet.
- Silhouette must read at 28×49 world pixels — no thin dangling accessories, no fine facial features.

### 6.7 UI illustration — native UI-pixel

- Sized for 1920×1080 HUD; kept out of the world viewport.
- Palette lock still applies. Anti-aliasing to sub-hues is not allowed.

---

## 7. Prompt templates

### Positive template

```
STYLE_ANCHOR
subject: <one-line subject>
pose: <pose>
palette: use ONLY these hex values — INK #292621, CHARCOAL #253333, PAPER #f2ead8,
CREAM #fff8e8, ZEST_YELLOW #e8b447, LEAF #4f7155, DEEP_LEAF #2f4a3c, MOSS #738c55,
RUST #a65338, RIVER #557c78, WORLD_WOOD #71543a, WORLD_SKIN #e7b990
composition: subject centered, feet at bottom edge of frame
background: fully transparent unless this asset is a scene background
delivery: PNG with straight alpha, no borders, no text, no ui
```

### Negative template

```
NEGATIVE_STYLE
```

---

## 8. Assets to produce next (priority order)

Each entry uses the template above with the class-specific brief in section 6.

1. **Weather-specific stand elements**
   - `prop_zest_stand_awning_rain_v01.png` — awning with rain sheet, drip line at edge.
     Class 6.3. Add subtle wet shine using CHARCOAL 20 % alpha only.
   - `prop_zest_stand_awning_sun_v01.png` — sunlit variant, SUNLIT_YELLOW rim on
     awning edge. Class 6.3.
2. **Vendor rain pose**
   - `vendor-idle-rain.png` — same skeleton as `vendor-idle.png`; adds a straw
     hat in WORLD_WOOD. Class 6.5.
3. **Commuter customer set**
   - Six files, class 6.6, subject: `office-worker commuter with coat and small bag`,
     accent color RUST for coat. Do not deliver east.
4. **Tourist customer set**
   - Six files, class 6.6, subject: `tourist with camera strap and hat`, accent
     color LEAF for hat. Do not deliver east.
5. **Strong Lemonade signage cue**
   - `prop_zest_stand_strong_menu_flag_v01.png`, class 6.3-scaled small flag,
     AMBER background, INK glyph "STRONG". Kept as a separate overlay layer so
     the base stand sprite is not modified.
6. **Second background variation for evening**
   - `bg_riverside_evening_640x360_v01.png`, class 6.1, warmer horizon,
     longer shadows toward top-right.

7. **Ambient life sprites** (reference: duck on pond, bird on bench)
   - `prop_park_duck_idle_v01.png` — 24×20 native, WORLD_WOOD beak, CREAM body,
     small AMBER tail highlight.
   - `prop_park_songbird_idle_v01.png` — 12×12 native, RUST breast, INK head.

8. **Signage set** (reference: chalkboards, banners, wooden signpost)
   - `sign_chalkboard_editorial_v01.png` — 48×40 native, INK slate, CREAM
     lettering, hand-lettered feel.
   - `sign_banner_editorial_v01.png` — 32×60 native, CREAM/ZEST_YELLOW banner
     hanging vertically, WORLD_WOOD frame.
   - `sign_signpost_v01.png` — 24×48 native, three horizontal wooden slats on a
     post, RUST arrows.

9. **Cobble path edging tile** — 16×8 native, irregular stones for path→grass
   transitions. Not required for MVP but strongly recommended for parity with
   the reference.

10. **Water elements** — `prop_water_lily_v01.png` 12×10 native, LEAF pad with
    tiny pink flower centre. `prop_water_cattail_v01.png` 8×24 native, DEEP_LEAF
    stem with WORLD_WOOD head.

For each requested asset produce a small contact sheet (four variants: at 100 %,
50 %, 25 % and at the exact runtime footprint from section 6) so approval can be
based on final-output size.

---

## 9. Directory and naming rules

```
game/art/production/
  ai-layered-v01/          ← AI-layered source assets (character, vendor, base stand)
    customer-<segment>-v01/
      customer-south-idle.png, walk-a.png, walk-b.png, north-walk-a.png,
      west-walk-a.png, west-walk-b.png, south-receive.png, west-leave.png,
      south-west-turn.png, west-south-turn.png
    vendor-poses/
      vendor-idle.png, vendor-prepare.png, vendor-pour.png, vendor-handoff.png,
      vendor-reach.png, vendor-cup-center.png
    stand-body.png
    weather-overlays/            ← NEW
      awning-rain.png, awning-sun.png
  final-grid-v03/                ← Native pixel props and stand upgrade atlas
    prop_zest_stand_base_idle_v03.png
    prop_zest_stand_better_counter_idle_v02.png
    prop_zest_stand_electric_juicer_idle_v02.png
    prop_zest_stand_bigger_cooler_idle_v02.png
    prop_zest_stand_upgrades_4x1_v01.png
  final-grid-v02/                ← Native pixel backgrounds and world props
    bg_riverside_640x360_v02.png
    prop_park_tree_idle_v02.png, prop_park_bench_idle_v02.png,
    prop_lamp_sign_idle_v02.png, prop_flower_planter_idle_v02.png,
    prop_bush_wind_4x1_v02.png
```

New assets must land in these paths. New folders (e.g. `weather-overlays/`)
must be added to the ProductionParkCanvas asset root constants when consumed.

---

## 10. Integration checklist (asset is "done" only when)

1. File is at the right path with the right name (section 9).
2. `.import` sidecar was generated via `tools/Zest.AssetPipeline` (never by hand).
3. `TextureFilter=Nearest`, `mipmaps=off`, `sRGB`.
4. Manual palette-lint: sample 20 random pixels — every one must map to a
   palette hex or its alpha.
5. Rendered at the exact runtime footprint (section 6) in a still frame from
   the game, reviewed visually. Dimensions alone do not count as approval.
6. Silhouette test: at runtime size, is the subject recognizable in one second?
7. Adjacent-asset test: placed beside its neighbors in the scene (stand +
   customer + vendor), does it read at the correct scale relation?
8. Regression: `dotnet test Zest.sln` still green; `dotnet build` no warnings.

---

## 11. What this document does not decide

- Marketing/store screenshots (different resolution and framing).
- Video/animation direction beyond the 2-frame walk / pose swaps already used.
- Voice, sound design, or music.
- Localization of any in-world signage — none exists yet in the world; keep it
  that way for now.
