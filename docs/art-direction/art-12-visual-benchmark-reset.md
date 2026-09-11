# ART-12 Visual Benchmark Reset

## Decision

The first ART-12 playable implementation is retained as a technical prototype,
not an approved visual slice. It met interaction and rendering checks but did
not meet the locked reference's authored detail, environmental density, or
silhouette quality.

The revised target separates **world placement grid** from **source visual
density**:

- placement grid remains 16 world units;
- environment tiles are authored at 32×32 source pixels;
- normal characters target 32×48 source pixels;
- the hero stand targets roughly 256×192 effective source pixels;
- large props may occupy several placement cells without reducing their pixel
  density;
- AI-generated imagery is reference or candidate material, never automatically
  accepted as a shipping sprite.

## Quality gate

Open `game/scenes/art12_visual_benchmark.tscn`. The initial view is the locked
world target. Press Tab to switch to the isolated HD stand candidate.

The stand must be approved before tree, character, ground, and path production
starts. Approval is visual, not merely technical:

1. readable ZEST landmark at gameplay scale;
2. clustered shading rather than large flat rectangles;
3. material separation between timber, cloth, glass, foliage, and fruit;
4. complete silhouette and clean alpha;
5. enough detail to coexist with the locked target without looking like a
   placeholder.

The old C# bitmap generators remain only for deterministic test/prototype
assets. They are not the production-art workflow for the high-density slice.
