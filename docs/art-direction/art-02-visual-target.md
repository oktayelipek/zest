# ART-02 — Visual Target Lock & Style Tokens

## Locked target

Zest uses a **warm pixel-inspired park tycoon** language: a tactile miniature
world, restrained citrus accents, readable human silhouettes, and a modern
editorial interface. The world borrows only the emotional warmth and immediate
readability associated with games such as Moonlighter. Geometry, stand
silhouette, palette, interaction hierarchy, and the park-business subject are
original to Zest.

This is not a pure retro-pixel game. Pixel rules belong to the world and its
feedback. Text-heavy planning, reports, and operational controls stay at native
UI resolution. It is also not a mobile reward surface or a SaaS dashboard:
there are no gem-like currencies, reward bursts, floating stat tiles, generic
charts, or interchangeable sidebar navigation. Information is framed as the
owner's plan, the live stand, and an editorial account of consequences.

## Production palette

| Token | Hex | Role |
| --- | --- | --- |
| `Ink` | `#292621` | Primary copy and silhouettes |
| `Charcoal` | `#253333` | Live operational band, deep contrast |
| `Paper` | `#F2EAD8` | Page ground |
| `Cream` | `#FFF8E8` | Panels, signs, warm highlights |
| `ZestYellow` | `#E8B447` | Brand landmark and primary action |
| `Leaf` | `#4F7155` | Positive/available/natural systems |
| `Rust` | `#A65338` | Pressure, loss, destructive action |
| `River` | `#557C78` | Neighborhood context and secondary info |
| `ProductBerry` | `#934F64` | Muted product-family accent |

World sprites may use the following locked tonal ramp without changing the
semantic UI colors: `DeepLeaf #2F4A3C`, `Moss #738C55`,
`LeafHighlight #A6B85E`, `DeepWood #4A3528`, `Amber #D68E2E`,
`WaterHighlight #7FA6A0`, `WorldShadow #3F4D3D`, `Stone #A49A85`, and
`SunlitYellow #EACB6A`. These shades create clustered pixel volume; they do not
become new UI state colors.

Yellow is a scarce landmark color, not a general reward color. Rust reports a
real operational consequence rather than manufacturing urgency. Product
accents never replace labels or state icons.

## Density and shape

- World source grid: 16 px. A normal character occupies roughly 16×24 source
  pixels; a stand module uses multiples of 16 px.
- Application/UI baseline: 1920×1080. The world remains a fixed 640×360
  viewport shown at 3×; UI remains at native desktop canvas resolution.
- At 720p/1440p/4K the same world is shown at exact 2×/4×/6× scale.
- Production world sprites are authored at final grid size and rendered at
  `Scale = 1`; high-resolution concept art is never fractionally downsampled at runtime.
- Spacing scale: 4, 8, 12, 18, 28, and 42 px.
- Radius scale: 8 px controls, 14 px panels, 18 px status pills.
- Borders are 1 px at rest and 2 px for focus/hover. Shadows are restrained,
  hard-edged 2–4 px offsets; blurred card stacks are not part of the language.

The canonical runtime values live in
`game/scripts/ZestStyle.cs`. New presentation code must consume these tokens
instead of introducing near-duplicate colors or arbitrary spacing.

## Typography

| Tier | Size | Use |
| --- | ---: | --- |
| Display | 40 | One editorial statement per page |
| Brand | 30 | Zest masthead only |
| Metric | 23 | Live quantities with nearby labels |
| Body | 17 | Guidance and report prose |
| Action | 15 | Buttons and interactive controls |
| Label | 12 | Kicker, section, state |
| Micro | 11 | Secondary metric labels |

UI copy uses a highly legible proportional face. Pixel fonts may be used for
short in-world signs only. Uppercase is limited to labels and wayfinding; body
copy uses sentence case.

## World and UI boundary

- World: nearest sampling, grid-aware motion, authored silhouettes, limited
  palette ramps, hard readable feedback.
- UI: native-resolution text, modern controls, editorial hierarchy, accessible
  contrast, no simulated pixel font at paragraph sizes.
- Zest Yellow, Leaf, Rust, Cream, and Charcoal cross the boundary so both layers
  feel authored for the same game.
- Operational truth is redundant: world animation carries the first signal and
  text/counts preserve accessibility. UI never hides a world problem behind a
  reward animation.

## Icon treatment

Icons use a 16 or 24 px optical box, a filled silhouette, one interior cut at
most, and square-ish terminals. Use `Ink` or `Cream` for neutral icons; reserve
Yellow/Leaf/Rust for state. Do not use glossy gradients, emoji, multicolor app
glyphs, or platform-generic outline icon packs.

## Acceptance lock

- Warm pixel-inspired tycoon target is explicit and bounded.
- Modern UI readability is preserved outside the pixel-rendered world.
- The dominant live surface is the park and stand, not dashboard chrome.
- Palette and interaction colors carry the same meaning in world and UI.
- Reference influence is limited to warmth/readability; Zest owns its citrus
  landmark, riverside-park context, silhouettes, and consequence-led language.
