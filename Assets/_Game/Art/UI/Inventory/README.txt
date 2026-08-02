Inventory UI art
================

Swap files anytime — keep the SAME names:

  Inventory_Frame.png       → full inventory panel background
  Item_Tooltip_Frame.png    → hover popup frame
  Icons/Icon_Sword.png      → rusty_sword (sample)
  Icons/Icon_Ring.png       → teal_ring (sample)
  Icons/Icon_Crystal.png    → moss_helm (sample placeholder)

How to replace
--------------
1. Overwrite PNG in this folder (Sprite 2D and UI / Single).
2. Run Tools → Idle RPG → Fix Hub UI Layout
   (binds frame + tooltip + item icons).

Runtime
-------
- Bag items fill the RIGHT grid (10 columns).
- Hover a cell → tooltip popup with name / rarity / ATK / HP.
- Click a bag cell → Equip into its slot.
- LEFT PaperDoll: 8 fixed boxes around the character
    Card / PaperDoll / Eq_Helmet, Eq_Armor, Eq_Legs, Eq_Accessory,
                      Eq_Shoulders, Eq_Weapon, Eq_Boots, Eq_Ring2
  Positions stay in Edit Mode (Fix Hub does not move existing boxes).
  Click equipped box → Unequip.
- Empty bag seeds 3 demo items once (can disable on InventoryPanelUI).

Edit Mode — bag item cells
--------------------------
Select: InventoryPanel / Card / BagSection / BagSlotTemplate (active in Edit Mode).

- Cell box size: BagContent → Grid Layout Group → Cell Size
  (runtime never overwrites this)
- Icon size: BagSlotTemplate / Icon RectTransform (anchors / offsets)
- Box image: BagSlotTemplate Image component (sprite + color)
  Runtime no longer clears this.

Play Mode hides the template; clones keep your settings.

No stacking: each item instance is its own cell (even duplicates).

Frame sharpness
---------------
Inventory_Frame.png is 1536x1024 (3:2). Runtime locks Card to 1080x720 so the art
fills without letterboxing. Dim overlay stays on so the village doesn't bleed through.
Import: Compression=None, Filter=Bilinear (or Point for sharper pixel look).
Keep panel scale = 1.
