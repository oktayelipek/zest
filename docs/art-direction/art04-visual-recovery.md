# ART-04 visual recovery — review target

Status: visual direction approved by the user; native production assets are not approved. The previous native atlas remains programmer art. Passing size, palette or build validation does not constitute visual acceptance.

## Godot resolution study

Run `game/scenes/approved_art_study.tscn` separately. Key 1 displays the approved reference; key 2 displays diagnostic sampling through a fixed 640x360 viewport at integer scale; Escape closes the study. The source is loaded from art-source only in this developer scene. Main gameplay is unchanged.

Verified: Debug game build passes; rendered 1920x1080 reference and 3x world study captures are in docs/artifacts/approved-art-reference-1080p.png and approved-art-world-study-1080p.png. This verifies the static study, not animation or shipping art. The stand including planters spans approximately 300 logical pixels in this composition, substantially larger than the planned 170-pixel asset. Thus this close-up cannot establish final 170x136 readability. Native authoring and the actual gameplay footprint remain outstanding.

## Deliverable

`art-source/concepts/art04/visual-target-v01/zest-park-visual-target.png`

Generated with the built-in image generator, using the user's detailed Zest kiosk as a design reference. This image is a composition and material reference only, not a game screenshot, native 640x360 scene, layered source, or validated 170x136 sprite. Do not run it through the runtime downsampling pipeline.

## Visual review

The rounded striped canopy, inset sign, counter depth, glass dispenser and citrus identity are retained. Quiet ground areas improve hierarchy. The customer and vendor have more coherent proportions than the programmer-art kiosk. Remaining problems: the kiosk occupies more of the composition than the intended 170/640 screen-width ratio, apparent pixel sizes vary, and fine sign/glass details still exceed the final-grid budget. Palette and grid conformity are not verified.

## Native production brief

Author a single base stand at 170x136, with a shared foot pivot. Preserve the reference silhouette and material separation; simplify tiny grain and emblem detail deliberately. Inspect at 1x and exact 3x inside the 640x360 world. Do not substitute resized concept pixels.

Separate editable layers: rear structure, canopy, counter/front, equipment, vendor behind counter, foliage, contact shadow. Customer remains an independent actor. Equipment upgrades share the base structure and pivot. Future animation should affect vendor service poses, canopy edges and foliage without moving the entire stand as one rigid image.

First approval scene: one stand, one customer, small path and grass patch. Use the actual gameplay framing rather than a promotional close-up. Check matching outline weight, lighting direction, character proportions, contact shadows and foreground overlap. Keep detailed operational text in the desktop UI or deliberate world labels outside the silhouette.

Only expand to upgrades and larger environment after this scene is visually accepted. Next technical checks: fixed logical world size at 1080p/1440p/4K, integer output scale, resizing/letterboxing, pointer coordinate mapping, and camera motion. No such cross-resolution or animation verification was performed for this concept.

## Generation specification

One 16:9 pixel-art concept for Project Zest. Reference kiosk: yellow/cream scalloped awning, warm oak counter, ZEST sign, glass lemonade dispenser, lemon crate and green-apron vendor. Intended composition: 640x360 gameplay scene with approximately 170x136 kiosk, one customer, calm lawn and sandy path, small bench/shrub and edge trees. Consistent slightly elevated orthographic perspective; deliberate clusters, selective outlines, restrained warm palette and sparse grass. No HUD, diagnostic labels, checkerboard or blur. Only text: ZEST. The generated result is a reference requiring review, not shipping art.
