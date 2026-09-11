# Current runtime visual contract

Status: active runtime contract, 2026-09-10. This document supersedes conflicting
runtime-production claims in TECHART-01, ART-02, ART-04 and ART-10. Those documents
remain useful as historical targets and source-pipeline references.

## Runtime authority

- The active playable world is assembled by `ProductionParkCanvas` and
  `ParkWorldView` from `game/art/production/`.
- The Base stand uses the previously user-approved AI-layered source in
  `game/art/production/ai-layered-v01/`; it is an approved runtime source, not a
  claim that the source is native pixel art.
- Better Counter, Electric Juicer and Bigger Cooler use the final-grid upgrade
  atlas. Their visual approval and their mechanical unlock status are separate.
- Native ART-10 exports and the Z-EN-01 ground remain opt-in comparison/proof
  assets until a visual review explicitly promotes them.

## Scaling and pixels

The shipped scene currently mixes sources with different native canvas sizes.
Fractional scale is therefore intentional at the source-to-world boundary:

| Element | Runtime rule |
| --- | --- |
| World composition | 640×360 logical world, nearest filtering |
| Stand | layered Base / final-grid upgrades, source-specific scale |
| Vendor | layered source-specific scale |
| Customer | shared HD source canvas at `0.035`; final world position is pixel-snapped |
| Native ART-10 assets | scale 1 only when used in their opt-in proof path |

The non-negotiable crispness rule is nearest filtering and whole-pixel final world
placement, not a blanket `Scale = 1` requirement for every imported source.

## Character terminology

- `16×24` is the historical ART-02 native character target.
- `32×48` is the ART-10 production-source target.
- The currently active HD layered customer canvas is neither target; its runtime
  scale is documented above. Do not use it to redefine either native target.

## Acceptance

A visual source becomes active only after a captured, final-output-size review.
Technical validation, image dimensions and an asset manifest alone do not grant
visual approval.
