# Student customer P0 set

This is the first high-density customer set for the approved Riverside composition. The shared 800x1400 canvas is sampled to approximately 49 logical world pixels at runtime and keeps a stable foot pivot.

Available poses:

- `customer-south-idle.png` and `customer-south-receive.png`
- `customer-south-walk-a.png` / `customer-south-walk-b.png`
- `customer-north-walk-a.png` / `customer-north-walk-b.png`
- `customer-west-walk-a.png` / `customer-west-walk-b.png` (east is mirrored at runtime; the pair uses opposite leg phases)
- `customer-west-leave.png` (post-sale carry pose; presentation hook pending)
- `customer-south-west-turn.png` / `customer-west-south-turn.png` (direction-change transition poses)

The master character uses a burgundy shirt, tan backpack, blue jeans and brown shoes. Generated images were normalized into the common canvas; baked checkerboard candidates were removed with the user-authorized deterministic cleanup tool. These are a first student segment pass, not the complete customer roster.

Walk-cycle QA note: `customer-west-walk-b.png` was regenerated after review because the earlier pair did not separate the leading legs clearly enough. The corrected B frame puts the image-left leg forward and the image-right leg back/lifted; A keeps the opposite phase.
