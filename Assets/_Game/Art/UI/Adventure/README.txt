Adventure UI art (Map Select)
=============================

Swap any file anytime — keep the SAME file names:

  Adventure_Frame.png         → panel frame / background
  Stage_Normal.png            → unlocked stage node
  Stage_Selected.png          → selected stage node
  Stage_Lock.png              → full art for locked stages
  Button_Enter.png            → Enter button art
  Button_Close.png            → Close (X) button art
  Chapter_Dropdown.png        → fallback closed dropdown bar
  Dropdown_Item_Normal.png    → one chapter row (normal)
  Dropdown_Item_Selected.png  → one chapter row (selected) + closed bar
  Adventure_Reference.png     → full mockup reference only (not used in runtime)

Chapter dropdown
----------------
Rows live in the scene (Edit Mode), not spawned at Play:
  AdventureCard / ChapterDropdown / ChapterList / Content / ChapterOption_1..N

Edit Mode tip:
1. Tools → Idle RPG → Fix Hub UI Layout  (creates missing ChapterOption_* rows)
2. Hierarchy'de ChapterList'i geçici olarak aç (checkmark)
3. ChapterOption_* RectTransform / Text / Image düzenle, Scene kaydet
4. ChapterList'i tekrar kapat (Play'de dropdown açınca görünür)

Runtime only refreshes label / sprite / locked — does not move rows.
  - Normal → Dropdown_Item_Normal.png + chapter name
  - Selected → Dropdown_Item_Selected.png + chapter name
Closed bar uses the selected art and shows the current chapter name.

How to replace in Unity
-----------------------
1. Export new art as PNG (transparent where needed).
2. Overwrite the matching file in this folder
   (or drag onto the file in the Project window).
3. Select the texture → Inspector:
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: Single
   - Filter Mode: Point (for pixel art) — optional
   - Apply
4. Run Tools → Idle RPG → Fix Hub UI Layout
   (or assign Sprite on CloseMapButton Image).

Close button
------------
File: Button_Close.png
Scene: ScreenCanvas / MapSelectPanel / AdventureCard / CloseMapButton
Click still closes the Adventure panel.

Layer boss node
---------------
Scene: AdventureCard / StagesRoot / Stage_LayerBoss
Label: LB — unlocks after clearing a chapter's stage 10 (chapter boss).
After LB: next chapter unlocks (or next layer if it was the last chapter).
Placement: move freely in Edit Mode — Fix Hub does not reposition it.
ENTER starts the layer boss (no summon stone cost for now).
Stage 10 never auto-skips into the next chapter.

Scene object
------------
ScreenCanvas / MapSelectPanel / AdventureCard
