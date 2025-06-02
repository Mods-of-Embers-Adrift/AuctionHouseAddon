# Auction House Type Sort Mod

## Overview

This mod enhances the Auction House in Embers Adrift by adding a powerful "Type" sorting option. With this feature, you can instantly group and sort all items for sale by their equipment type—such as weapon class, armor slot, or jewelry—making it much easier to browse, price, and find the items you want.

**Key Features:**
- Adds "Type" as a new sort option in the Auction House dropdown.
- Instantly sorts all items by weapon type (e.g., Sword1H, Axe2H, Bow, Shield, etc.), then by armor slot (e.g., Head, Chest, Legs), then by jewelry (Necklace, Ring, Earring).
- Sorts by auction expiration within each item type group, so soon-to-expire items are always visible at the top of their group.
- Updates the Auction House UI immediately after sorting—no need to scroll to see changes.

---

## Supported Sort Order

When "Type" is selected, the list is grouped and sorted in this order (and within each type, by expiration):

**Weapons:**
1. Sword1H
2. Sword2H
3. Axe1H
4. Axe2H
5. Dagger
6. Rapier
7. Polearm
8. Spear
9. Mace1H
10. Mace2H
11. Hammer1H
12. Hammer2H
13. Staff1H
14. Staff2H
15. Bow
16. Crossbow
17. Shield
18. OffhandAccessory
19. Other weapons

**Armor:**
- Helm (Head/Mask)
- Pauldrons (Shoulders)
- Chest/Tunic
- Vambraces (Hands)
- Greaves/Trousers (Legs)
- Boots (Feet)
- Faulds (Waist)
- Other armor

**Jewelry:**
- Necklace
- Ring
- Earring

**Everything else** (recipes, consumables, etc.): Bottom of the list

---

## Installation

1. Place the compiled DLL in your game's `Mods` folder.
2. Start Embers Adrift with MelonLoader.
3. Open the Auction House and select "Type" from the sort dropdown.

---

## Changelog

### v1.0.0
- Initial release. Adds a "Swords" sort to bring all swords to the top of the list.

### v1.1.0
- Reworked to add a generic "Type" sort option (instead of just swords).
- Now supports all weapon types, all armor slots, and jewelry.
- Full sort order groups by equipment type, then by soonest expiration.
- Forces instant UI update after sorting (no need to scroll to see results).

### v1.1.1
- Code refactor and cleanup.
- Improved type detection and category handling.
- README updated with full feature description and changelog.

---

## Credits

Created by MrJambix, with assist from GitHub Copilot.

Suggestions, bug reports, and PRs welcome!