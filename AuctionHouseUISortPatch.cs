using MelonLoader;
using HarmonyLib;
using SoL.Game.AuctionHouse;
using SoL.Game.Objects.Archetypes;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using SoL.Networking.Database;

/// <summary>
/// Adds a "Type" sort to the Auction House, grouping by weapon/armor/jewelry type,
/// and sorting by expiration within each group. Instantly updates UI.
/// </summary>
public class AuctionHouseTypeSortMod : MelonMod
{
    public override void OnInitializeMelon() => HarmonyInstance.PatchAll();
}

// Insert "Type" into the dropdown after "Item Name" (index 0)
[HarmonyPatch(typeof(AuctionHouseForSaleList), "Awake")]
public static class AuctionHouseTypeSort_AwakePatch
{
    static void Postfix(AuctionHouseForSaleList __instance)
    {
        var dropdownField = __instance.GetType().GetField("m_sortTypeDropdown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dropdown = dropdownField?.GetValue(__instance) as TMP_Dropdown;
        if (dropdown == null) return;

        int insertAt = 1;
        if (!dropdown.options.Any(o => o.text == "Type"))
        {
            dropdown.options.Insert(insertAt, new TMP_Dropdown.OptionData("Type"));
            dropdown.RefreshShownValue();
        }
    }
}

// Patch RefreshFilteredList: forcibly sort by type if "Type" is selected
[HarmonyPatch(typeof(AuctionHouseForSaleList), "RefreshFilteredList")]
public static class AuctionHouseTypeSort_RefreshFilteredListPatch
{
    static void Postfix(AuctionHouseForSaleList __instance)
    {
        var dropdownField = __instance.GetType().GetField("m_sortTypeDropdown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dropdown = dropdownField?.GetValue(__instance) as TMP_Dropdown;
        if (dropdown == null) return;

        var filteredField = __instance.GetType().GetField("m_filteredAuctions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filtered = filteredField?.GetValue(__instance) as List<AuctionRecord>;
        if (filtered == null) return;

        int typeIndex = dropdown.options.FindIndex(o => o.text == "Type");
        if (typeIndex < 0 || dropdown.value != typeIndex) return;

        filtered.Sort((a, b) =>
        {
            int typeA = GetSortCategory(a);
            int typeB = GetSortCategory(b);
            if (typeA != typeB) return typeA.CompareTo(typeB);
            // Sort by expiration within each group
            return a.Expiration.CompareTo(b.Expiration);
        });

        // Force OSA to rebuild the UI so the sort is shown immediately
        __instance.ResetItems(filtered.Count, false, false);
    }

    // Assign a category number by type. Lower = higher up in list.
    static int GetSortCategory(AuctionRecord record)
    {
        var archetype = record?.Instance?.Archetype;

        // --- Weapon Types ---
        if (archetype is WeaponItem weapon)
        {
            switch (weapon.GetWeaponType())
            {
                case WeaponTypes.Sword1H: return 0;
                case WeaponTypes.Sword2H: return 1;
                case WeaponTypes.Axe1H: return 2;
                case WeaponTypes.Axe2H: return 3;
                case WeaponTypes.Dagger: return 4;
                case WeaponTypes.Rapier: return 5;
                case WeaponTypes.Polearm: return 6;
                case WeaponTypes.Spear: return 7;
                case WeaponTypes.Mace1H: return 8;
                case WeaponTypes.Mace2H: return 9;
                case WeaponTypes.Hammer1H: return 10;
                case WeaponTypes.Hammer2H: return 11;
                case WeaponTypes.Staff1H: return 12;
                case WeaponTypes.Staff2H: return 13;
                case WeaponTypes.Bow: return 14;
                case WeaponTypes.Crossbow: return 15;
                case WeaponTypes.Shield: return 16;
                case WeaponTypes.OffhandAccessory: return 17;
                default: return 30; // Other weapon
            }
        }

        // --- Armor Types ---
        if (archetype is ArmorItem armor)
        {
            switch (armor.Type)
            {
                case EquipmentType.Head:
                case EquipmentType.Mask: return 40;
                case EquipmentType.Armor_Shoulders: return 41;
                case EquipmentType.Armor_Chest:
                case EquipmentType.Clothing_Chest: return 42;
                case EquipmentType.Armor_Hands:
                case EquipmentType.Clothing_Hands: return 43;
                case EquipmentType.Armor_Legs:
                case EquipmentType.Clothing_Legs: return 44;
                case EquipmentType.Armor_Feet:
                case EquipmentType.Clothing_Feet: return 45;
                case EquipmentType.Waist: return 46;
                default: return 50; // Other armor
            }
        }

        // --- Jewelry Types ---
        var typeProp = archetype?.GetType().GetProperty("Type");
        if (typeProp != null)
        {
            var eqTypeObj = typeProp.GetValue(archetype);
            if (eqTypeObj is EquipmentType eqType)
            {
                switch (eqType)
                {
                    case EquipmentType.Jewelry_Necklace: return 60;
                    case EquipmentType.Jewelry_Ring: return 61;
                    case EquipmentType.Jewelry_Earring: return 62;
                }
            }
        }

        // --- Fallback ---
        return 999;
    }
}