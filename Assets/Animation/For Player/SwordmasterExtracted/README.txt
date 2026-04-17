Swordmaster Animation Extractor

This folder is meant for writable .anim files copied out of the read-only
.aseprite imports under:

- Assets/Sprites/The SwordMaster/Sword Master Sprite Sheet.aseprite
- Assets/Sprites/The SwordMaster/Sword Master Sprite Sheet Ledge Climb.aseprite

How to use:

1. Open Unity.
2. Wait for compilation to finish.
3. Run:
   Tools > Swordmaster > Extract Generated Animation Clips
4. Unity will copy generated AnimationClip sub-assets into this folder as
   standalone .anim files.

Why this exists:

- Generated clips inside .aseprite imports are read-only.
- Extracted .anim files can be edited normally.
- You can add Animation Events, rename clips, adjust samples, loop settings,
  and assign them to your own Animator Controller.

Suggested next step:

- Use the extracted clips in your player Animator instead of editing the
  imported .aseprite sub-assets directly.
