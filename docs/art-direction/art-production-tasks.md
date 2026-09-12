# Zest — art production tasks

Tracks every asset the codebase already has a slot for. Each row lists the
target file path (drop the PNG there and it lights up automatically), a ready-
to-paste Gemini prompt, and a status checkbox. Update the checkbox when the
PNG is committed.

**Prerequisites for every prompt**

- Attach `docs/art-direction/style-anchor/style-anchor-v1.png` as an image
  reference. Text alone will not match the style.
- Paste the shared header before each prompt (see `Shared header` below).
- Deliver PNG at the exact pixel size stated. Transparent background unless the
  row says "opaque scene".
- After you drop the PNG, open the game once so Godot generates the `.import`
  sidecar; commit both.

---

## Shared header (paste before every prompt)

```
Match the exact art style, palette, outline weight and cozy warmth of the
attached reference image (Zest lemonade stand park scene). Keep every color
within this strict 16-color palette and do not invent new hues:
INK #292621, CHARCOAL #253333, PAPER #f2ead8, CREAM #fff8e8,
ZEST_YELLOW #e8b447, LEAF #4f7155, DEEP_LEAF #2f4a3c, MOSS #738c55,
RUST #a65338, RIVER #557c78, WORLD_WOOD #71543a, WORLD_SKIN #e7b990,
AMBER #d68e2e, SUNLIT_YELLOW #eacb6a, WORLD_PATH #d8c9aa, WORLD_GROUND #91a76f.
Output a single PNG at the exact pixel size I specify, transparent background
unless I explicitly ask for a full scene. No text, no watermark, no UI, no HUD,
no border, no signature. No photorealism, no 3D render, no anime, no chibi.
```

---

## Batch A — weather + stand cues (highest impact, smallest files)

- [ ] **A1. Rain awning overlay**
  - Path: `game/art/production/ai-layered-v01/weather-overlays/awning-rain.png`
  - Size: 170×136, transparent
  - Prompt:
    ```
    Draw a single asset in the style of the attached reference image: the yellow-
    and-cream striped awning of a small park lemonade stand, seen from the front.
    It is raining — a thin sheet of rain cascades down the front edge and small
    water drips hang at each stripe seam. Add a subtle wet shine highlight on the
    awning surface. Do not include the stand body, do not include any character or
    sign — the awning shape only. Output at exactly 170×136 pixels with a fully
    transparent background. Center the awning horizontally.
    ```

- [x] **A2. Sun awning overlay**
  - Path: `game/art/production/ai-layered-v01/weather-overlays/awning-sun.png`
  - Size: 170×136, transparent
  - Prompt:
    ```
    Same striped yellow-and-cream lemonade stand awning as in the previous request,
    but this time in warm bright afternoon sunlight. Add a soft golden rim glow
    along the top edge of the awning and a gentle warm halo above it. Same warm
    cozy pixel style as the reference. Awning shape only, no stand body, no
    characters, no signs. Output at exactly 170×136 pixels with a fully transparent
    background.
    ```

- [ ] **A3. Vendor rain hat**
  - Path: `game/art/production/ai-layered-v01/weather-overlays/vendor-rain-hat.png`
  - Size: 200×200, transparent
  - Prompt:
    ```
    Draw a single small wide-brim rain hat in warm dark brown wood tones, worn by
    an unseen child-height vendor. The hat is shown from the same top-down 3/4
    angle as characters in the reference image. Brim visible from front, crown
    with a subtle lighter highlight along the top. Just the hat — no head, no
    face, no body underneath. Output at exactly 200×200 pixels with a fully
    transparent background.
    ```

- [ ] **A4. Strong Lemonade menu flag**
  - Path: `game/art/production/ai-layered-v01/stand-overlays/strong-menu-flag.png`
  - Size: 96×80, transparent
  - Prompt:
    ```
    Draw a small triangular pennant flag in warm amber on a short wooden pole,
    in the exact cozy pixel style of the reference image. The pennant carries
    hand-lettered "STRONG" text in dark ink. The pole is warm brown wood, base
    at the bottom center of the frame. Output at exactly 96×80 pixels with a
    fully transparent background. Do not draw any other object.
    ```

---

## Batch B — evening scene

- [ ] **B1. Evening background**
  - Path: `game/art/production/final-grid-v02/bg_riverside_evening_640x360_v01.png`
  - Size: 640×360, opaque scene
  - Prompt:
    ```
    Redraw the exact same riverside park scene as the attached reference image,
    but at late afternoon into early evening. The sky glows warm amber-orange
    along the horizon, shadows are longer and cast toward the top-right, the
    water reflects the warm sky, foliage silhouettes are slightly darker, grass
    and path tones warm up. Keep the same composition, same tree positions, same
    pond, same cobble-bordered dirt path — but remove all characters, remove the
    lemonade stand, remove all signage. This is the empty scene background only.
    Output at exactly 640×360 pixels, opaque (no transparency), same warm cozy
    pixel style as the reference.
    ```

---

## Batch C — ambient life (small, warm)

- [ ] **C1. Pond duck**
  - Path: `game/art/production/final-grid-v02/prop_park_duck_idle_v01.png`
  - Size: 24×20, transparent
  - Prompt:
    ```
    Draw a single tiny white duck floating calmly, seen from the same top-down 3/4
    angle as animals would sit in the reference image. Warm wood-colored beak, a
    small amber highlight on the tail feathers, calm expression, clean silhouette.
    No water beneath — just the duck. Output at exactly 24×20 pixels with a fully
    transparent background.
    ```
  - Placement note: also needs a small code slot; ping me after the PNG lands.

- [ ] **C2. Bench songbird**
  - Path: `game/art/production/final-grid-v02/prop_park_songbird_idle_v01.png`
  - Size: 12×12, transparent
  - Prompt:
    ```
    Draw a single tiny songbird perched, side view, in the cozy pixel style of the
    reference image. Rust-colored breast, dark ink head, tiny black eye. No perch,
    no branch — just the bird. It must read clearly at this very small size.
    Output at exactly 12×12 pixels with a fully transparent background.
    ```
  - Placement note: needs a small code slot; ping me after the PNG lands.

---

## Batch D — signage (brings the reference "voice" to the park)

- [ ] **D1. A-frame chalkboard**
  - Path: `game/art/production/final-grid-v02/sign_chalkboard_editorial_v01.png`
  - Size: 48×40, transparent
  - Prompt:
    ```
    Draw a small A-frame chalkboard sign on two wooden legs, in the exact cozy
    pixel style of the reference image. Dark charcoal slate face with hand-
    lettered cream text reading "GOOD PEOPLE" on the top line and "BRIGHTER DAYS"
    on the bottom line. Warm brown wooden frame around the slate. Subtle chalk
    texture visible. Output at exactly 48×40 pixels with a fully transparent
    background. Feet of the A-frame touch the bottom edge of the frame.
    ```

- [ ] **D2. Vertical banner**
  - Path: `game/art/production/final-grid-v02/sign_banner_editorial_v01.png`
  - Size: 32×60, transparent
  - Prompt:
    ```
    Draw a small vertical hanging cloth banner, in the cozy pixel style of the
    reference image. Warm cream background fabric, hand-lettered warm-ink text
    reading "SMALLER SIPS" on top and "BRIGHTER DAYS" below, with a tiny heart
    glyph at the bottom. The banner hangs from a short warm-brown wooden crossbar
    at the top of the frame. Output at exactly 32×60 pixels with a fully
    transparent background.
    ```

- [ ] **D3. Trail signpost**
  - Path: `game/art/production/final-grid-v02/sign_signpost_v01.png`
  - Size: 24×48, transparent
  - Prompt:
    ```
    Draw a wooden trail signpost with three horizontal wooden slats stacked on a
    vertical post, in the cozy pixel style of the reference image. Top slat reads
    "RIVER →", middle slat "PARK ♥", bottom slat "KINDER PEOPLE", all in hand-
    lettered dark ink on warm brown wood. Post base at the bottom center of the
    frame. Output at exactly 24×48 pixels with a fully transparent background.
    ```

---

## Batch E — water elements

- [ ] **E1. Lily pad**
  - Path: `game/art/production/final-grid-v02/prop_water_lily_v01.png`
  - Size: 12×10, transparent
  - Prompt:
    ```
    Draw a single small green lily pad seen from above, with a tiny pink flower
    dot at its center, in the cozy pixel style of the reference image. No water
    beneath, no other elements. Output at exactly 12×10 pixels with a fully
    transparent background.
    ```

- [ ] **E2. Cattail reed**
  - Path: `game/art/production/final-grid-v02/prop_water_cattail_v01.png`
  - Size: 8×24, transparent
  - Prompt:
    ```
    Draw a single tall cattail reed, side view: dark leafy green stem with a warm
    brown cylindrical cattail head near the top. Tall and thin silhouette in the
    cozy pixel style of the reference image. No water, no ground. Output at
    exactly 8×24 pixels with a fully transparent background. Stem base at the
    bottom edge of the frame.
    ```

---

## Batch F — customer variants (largest; leave for last)

Two segments × 10 poses each = 20 files at 800×1400 pixels, transparent PNG.

### Shared per-file prefix

```
Draw a single character isolated on a fully transparent background, in the
exact cozy pixel style of the reference image (Zest park scene). Same top-
down 3/4 view as the characters in the reference, feet slightly angled
forward, contact-shadow ellipse painted at the feet. Character carries a
chunky near-black outline. Output at 800×1400 pixels — the character will be
downsampled to about 28×49 pixels in-game, so the silhouette must read
clearly at that small size. No fine facial detail, no thin dangling
accessories, no held objects unless the pose asks for one.
```

Then append: `Subject: {segment description}. Pose: {pose row}.`

### F1 — Commuter (10 files)

- Path root: `game/art/production/ai-layered-v01/customer-commuter-v01/`
- Subject line: `office-worker commuter in early 30s, warm rust-colored coat, dark trousers, small leather satchel over one shoulder, tidy short hair, calm tired expression`

| # | Filename | Pose line |
|---|---|---|
| [ ] | `customer-south-idle.png` | facing camera, arms relaxed at sides, standing still |
| [ ] | `customer-south-walk-a.png` | facing camera, mid-stride with left foot forward |
| [ ] | `customer-south-walk-b.png` | facing camera, mid-stride with right foot forward |
| [ ] | `customer-north-walk-a.png` | facing away from camera, mid-stride with left foot forward |
| [ ] | `customer-north-walk-b.png` | facing away from camera, mid-stride with right foot forward |
| [ ] | `customer-west-walk-a.png` | side profile facing left, walking cycle frame A |
| [ ] | `customer-west-walk-b.png` | side profile facing left, walking cycle frame B |
| [ ] | `customer-south-receive.png` | facing camera, both arms extended forward receiving a cup |
| [ ] | `customer-west-leave.png` | facing left, walking away while holding a paper cup in one hand |
| [ ] | `customer-south-west-turn.png` | mid-turn between facing camera and facing left |
| [ ] | `customer-west-south-turn.png` | mid-turn between facing left and facing camera |

### F2 — Tourist (10 files)

- Path root: `game/art/production/ai-layered-v01/customer-tourist-v01/`
- Subject line: `casual tourist in mid 20s, cream shirt, leaf-green wide-brim sun hat, camera strap crossing chest, small daypack, curious eager expression`
- Same 10 poses as F1 (same filenames, same pose lines).

---

## Iteration tips (paste into Gemini when needed)

- Style drift: `"regenerate matching the attached reference exactly — same warm palette, chunky character outline, no new hues"`
- Palette drift: `"tighten the palette to only the hex list I provided; recolor any pixel outside that palette to the nearest listed color"`
- Wrong size: `"regenerate at exactly WxH pixels"` (if Gemini still won't, resize in Preview/Photoshop with nearest-neighbor scaling)
- Opaque background: `"remove the background entirely, transparent PNG only, subject cutout"`
- Face detail overload: `"keep the face very simple — only two eye dots and a small mouth line"`

## Quality gate (before committing)

1. Silhouette test at runtime footprint (see brief section 6 for target size).
2. Palette spot-check: sample 20 random pixels; each should be on-palette.
3. Side-by-side with `style-anchor-v1.png` — does it feel like the same world?
4. `dotnet test Zest.sln` and `dotnet build Zest.sln` still green.

If any check fails, iterate — do not commit the miss.

---

## Delivery checklist per file

1. PNG lands at the exact path in the row above.
2. `.import` sidecar generated (open the game once in Godot editor).
3. Both files committed.
4. Row checkbox flipped in this document.
5. If the row has a "Placement note", ping the code side to wire the new asset
   into a scene node (rows without a note use a slot that already exists).
