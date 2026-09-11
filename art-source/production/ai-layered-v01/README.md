# Zest stand body — AI-assisted layer candidate

The user approved AI-assisted runtime layers and subsequently approved local background removal.

Source: art-source/concepts/art04/layer-candidates-v01/stand-no-vendor-opaque.png.
Image generation produced a baked checkerboard. Its RGBA-converted intermediate is stand-no-vendor-rgba.png beside that source. Run `dotnet run --project tools/Zest.AssetPipeline -- clean-stand-layer` to rebuild the cutout and green-background proof.

Removal rule: flood fill from image borders through neutral light pixels only (minimum RGB 115, channel spread at most 30). Retained RGBA values are unchanged. No resizing, sharpening, palette conversion or drawing occurs. Output: 1536x1024 RGBA, 543560 transparent pixels. The reference proof is docs/artifacts/stand-body-alpha-proof.png.

Godot reusable scene: game/scenes/zest_stand_body.tscn. The body is displayed proportionally at 170 logical pixels wide, with an approximate ground pivot and empty VendorAnchor. It is an AI-assisted high-resolution source sampled at runtime, not native 170x136 pixel art. Main gameplay still uses the previous stand until the vendor layer and complete in-context assembly are ready. This body contains the equipment and planters; they are not yet separately animatable.

P0 vendor presentation uses six normalized 720x850 transparent canvases under `vendor-poses/`: idle, reach, prepare, pour, cup-center and handoff. Generated action images arrived with a baked neutral checkerboard; `dotnet run --project tools/Zest.AssetPipeline -- clean-vendor-poses` removes only the border-connected neutral component. The cleaned visible figures were normalized to the idle figure's 833-pixel height and centered without distortion. `docs/artifacts/vendor-animation-poses-v02.png` is the current QA contact sheet.

The vendor is layered between the stand body and `stand-front-occluder.png`; the occluder republishes unchanged body pixels from source row 720 downward. These layers share the stand's Y-sort level so the fascia hides the vendor torso without incorrectly covering customers standing in front. `VendorVisual` exposes named presentation poses without placing simulation state in Godot nodes. Prepare loops between reach and stir; handoff moves through cup-center before extending; idle uses a whole-logical-pixel breathing bob. The integrated Base stand is active in the main world; the old atlas remains the temporary fallback for three upgrade states.

Transition generation prompts (built-in Imagegen):

- Reach: preserve the exact vendor identity, proportions, costume and pixel-inspired raster style; create a halfway pose from idle to prepare with the right hand reaching for the spoon and left hand moving toward the small cup; genuine transparent alpha; no stand, scenery, text, shadow or extra limbs.
- Cup-center: preserve the exact vendor identity, proportions, costume and pixel-inspired raster style; after pouring and before handoff, remove the pitcher and hold exactly one full lemonade cup with both hands at chest level; genuine transparent alpha; no stand, scenery, text, shadow or extra limbs.

Generation brief: isolate the approved warm oak Zest kiosk, rounded yellow/cream awning, ZEST sign, lemon badge, dispenser, cups, lemon crate, front banner and planters; reconstruct the front obscured by the customer; leave the center opening empty; remove people and exterior scenery. Built-in image generation was used, followed by user-authorized local alpha cleanup.
