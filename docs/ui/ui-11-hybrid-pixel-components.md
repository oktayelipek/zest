# UI-11 — Hybrid Pixel UI Skin & Component Rules

## Intent

Zest uses crisp, hard-edged component frames around modern readable type. The
skin borrows the world's restrained citrus, leaf, wood, and river colors but
does not imitate a retro RPG inventory. Live operational overlays and editorial
planning/report screens use the same component family.

## Component contract

| Component | Godot implementation | Figma mapping |
| --- | --- | --- |
| Surface | `ZestUiSkin.Panel` + scalable `StyleBoxFlat` | Frame with Auto Layout, 8/14 px radius |
| Button | `ZestUiSkin.ApplyButton` | Component set: normal, hover, pressed, focus, disabled |
| Selected control | Toggle `ButtonPressed` using the pressed token | Boolean `selected` variant |
| Tooltip | Shared `TooltipPanel`/`TooltipLabel` theme | Dark overlay, 9 px padding, 15 px type |
| Status bubble | Pill-radius themed Label | Content-sized Auto Layout frame |
| Separator | 1 px `Border` token | 1 px line using Border color |

Frames are scalable UI geometry, not raster textures. This is the Godot
equivalent of a 9-slice contract: corners retain their radius/border while the
center and content margins grow with text and localization.

## Interaction states

- **Normal:** one-pixel neutral border.
- **Hover:** eight-percent lighter fill and two-pixel Zest Yellow border.
- **Pressed/selected:** nine-percent darker fill and two-pixel Ink border.
- **Keyboard focus:** transparent fill with two-pixel Zest Yellow focus ring.
- **Disabled:** 56% surface opacity, muted copy, and subdued border.
- **Warning:** Rust surface with Cream text; it reports a real consequence.

Every actionable control remains keyboard-focusable. Tooltips supplement short
hotbar/time labels but never carry required operational truth by themselves.

## Typography and scaling

Body text stays proportional and native-resolution. Pixel fonts are limited to
short world-space signs. UI uses the `ZestStyle.Type` scale at the 1920×1080
baseline and relies on Containers/anchors rather than raster upscaling.

## Proof in the playable slice

- Speed controls demonstrate normal, hover, pressed, focus, disabled-ready,
  selected, and tooltip behavior.
- Stand interventions demonstrate primary, positive, warning, and disabled
  semantic variants.
- Live context panels, Notebook, morning planning, and daily report cards all
  call the same scalable panel and button primitives.
