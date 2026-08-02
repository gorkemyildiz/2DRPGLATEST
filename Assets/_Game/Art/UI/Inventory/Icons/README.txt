Item icons (bag + paper doll)
=============================

Problem
-------
Cinematic 1536x1024 renders do NOT work as inventory icons.
In a 40x40 cell they become an unreadable speck.

Correct icon format
-------------------
- Square PNG: 256x256 (or 128x128)
- Transparent background (alpha), not black
- Subject fills ~70–90% of the frame (centered)
- Clear silhouette readable at small size
- Style: simple/readable > photoreal detail

Files used in game
------------------
  Icon_Sword.png    → rusty_sword
  Icon_Ring.png     → teal_ring
  Icon_Crystal.png  → moss_helm

Unity import (Inspector)
------------------------
Texture Type: Sprite (2D and UI)
Max Size: 256
Compression: None
Filter Mode: Bilinear
Alpha Is Transparency: on

How to add a new item icon
--------------------------
1. Create/export a 256x256 transparent PNG.
2. Drop it here (or overwrite an Icon_*.png).
3. On the Item ScriptableObject, assign that sprite to Icon.
4. Optional: Tools → Idle RPG → Fix Hub UI Layout (binds samples).

Source/ holds the original wide renders (not used in UI).
