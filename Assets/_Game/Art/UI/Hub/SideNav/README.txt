VillageHub left icon rail (SideNav)
===================================

Swap icons anytime — keep the SAME file names:

  Icon_Adventure.png   → Adventure / map
  Icon_Village.png     → Village buildings
  Icon_Inventory.png   → Inventory / bag
  Icon_Quit.png        → Quit

How to replace in Unity
-----------------------
1. Export your new art as PNG (transparent background recommended).
2. Overwrite the matching file in this folder
   (or drag-drop onto the file in the Project window).
3. Select the texture → Inspector:
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: Single
   - Apply
4. No code / wizard re-run needed if the Image still points here.
   If the button looks blank: select SideNav → AdventureIcon (etc.)
   → Image → Sprite → pick your new sprite.

Tips
----
- Square icons work best (e.g. 256x256 or 512x512).
- Selected icon is scaled larger in play mode (HubSideNavUI).
- SideNav object lives under: ScreenCanvas / HubMenu / SideNav
