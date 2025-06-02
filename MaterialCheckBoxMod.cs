using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using SoL.Game;
using SoL.Game.AuctionHouse;
using SoL.Game.Objects.Archetypes;
using SoL.Networking.Database;
using SoL;

// This mod adds a "Material" checkbox filter to the Auction House UI.
// When checked, only items considered "materials" (as defined by name match against a material list) will be shown.
// The checkbox is OFF by default, and its state is persisted with MelonPreferences.

public class MaterialCheckBoxMod : MelonMod
{
    internal static MelonPreferences_Category prefsCategory;
    internal static MelonPreferences_Entry<bool> materialCheckboxValue;

    public override void OnInitializeMelon()
    {
        prefsCategory = MelonPreferences.CreateCategory("MaterialCheckBoxMod", "Auction House Mod Settings");
        materialCheckboxValue = prefsCategory.CreateEntry("MaterialCheckbox", false, "Material Checkbox State (default: off)");
    }
}

// Injects the "Material" checkbox into the Auction House filter panel
[HarmonyPatch(typeof(AuctionHouseUI), "UIWindowOnShowCalled")]
public static class AuctionHouseUIMaterialPatch
{
    static void Postfix(AuctionHouseUI __instance)
    {
        var ahGO = __instance.gameObject;
        Transform filterPanel = null;

        // Try likely panel names; adjust if you find the exact filter panel
        foreach (var childName in new[] { "WindowContent", "TopPanelContent" })
        {
            var child = ahGO.transform.Find(childName);
            if (child != null && child.GetComponentsInChildren<Toggle>(true).Length > 0)
            {
                filterPanel = child;
                break;
            }
        }
        if (filterPanel == null)
        {
            MelonLogger.Warning("Auction House filter panel not found!");
            return;
        }

        // Find the "Pinned" toggle to clone and place Material to its right
        var pinnedToggle = filterPanel.GetComponentsInChildren<Toggle>(true)
            .FirstOrDefault(t =>
            {
                var label = t.GetComponentInChildren<TextMeshProUGUI>();
                return (t.gameObject.name.Contains("Pinned") ||
                        (label != null && label.text.Trim() == "Pinned"));
            });
        if (pinnedToggle == null)
        {
            MelonLogger.Warning("Pinned toggle not found!");
            return;
        }

        // Prevent duplicate
        if (filterPanel.Find("MaterialToggle") != null)
            return;

        // Clone the Pinned toggle for Material
        var newToggleObj = Object.Instantiate(pinnedToggle.gameObject, pinnedToggle.transform.parent);
        newToggleObj.name = "MaterialToggle";
        var newToggle = newToggleObj.GetComponent<Toggle>();
        var labelObj = newToggleObj.GetComponentInChildren<TextMeshProUGUI>();
        if (labelObj != null)
            labelObj.text = "Material";

        // Place right after "Pinned" (to the right in horizontal layout)
        int pinnedIndex = pinnedToggle.transform.GetSiblingIndex();
        newToggleObj.transform.SetSiblingIndex(pinnedIndex + 1);

        // (Optional) If visually overlapping, try to adjust position (for non-layout-group UIs)
        var rt = newToggleObj.GetComponent<RectTransform>();
        if (rt != null && pinnedToggle.GetComponent<RectTransform>() != null)
        {
            var parentLayout = pinnedToggle.transform.parent.GetComponent<HorizontalLayoutGroup>();
            if (parentLayout == null)
            {
                rt.anchoredPosition = pinnedToggle.GetComponent<RectTransform>().anchoredPosition +
                                     new Vector2(rt.sizeDelta.x + 20f, 0);
            }
        }

        // Set persistent value and force UI refresh on change
        newToggle.isOn = MaterialCheckBoxMod.materialCheckboxValue.Value;
        newToggle.onValueChanged.RemoveAllListeners();
        newToggle.onValueChanged.AddListener((value) =>
        {
            MaterialCheckBoxMod.materialCheckboxValue.Value = value;
            MelonLogger.Msg($"Material Checkbox toggled: {value}");

            // Force update the Auction House UI to refresh the list immediately using public method
            var auctionHouseUI = Object.FindObjectOfType<AuctionHouseUI>();
            if (auctionHouseUI != null)
            {
                auctionHouseUI.UpdateAuctionList(null);
                MelonLogger.Msg("Auction House UI refreshed via UpdateAuctionList(null);");
            }
        });

        MelonLogger.Msg("Material Checkbox injected to the right of Pinned!");
    }
}

// Filters the Auction House list if the material checkbox is enabled
[HarmonyPatch(typeof(AuctionHouseForSaleList), "RefreshFilteredList")]
public static class AuctionHouseMaterialFilterPatch
{
    static void Postfix(AuctionHouseForSaleList __instance)
    {
        if (!MaterialCheckBoxMod.materialCheckboxValue.Value)
            return;

        // Get m_filteredAuctions (the current filtered auction list)
        var filteredField = __instance.GetType().GetField("m_filteredAuctions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filtered = filteredField?.GetValue(__instance) as List<AuctionRecord>;
        if (filtered == null) return;

        filtered.RemoveAll(x => !MaterialFilterHelper.IsMaterial(x));
        __instance.ResetItems(filtered.Count, false, false);
    }
}

// Helper class for material detection using known material names
public static class MaterialFilterHelper
{
    // List of all material names (raw and refined forms)
    private static readonly HashSet<string> MaterialNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "Copper Ore", "Copper Ingot",
        "Iron Ore", "Iron Ingot",
        "Mithril Ore", "Mithril Ingot",
        "Silver Ore", "Silver Ingot",
        "Gold Ore", "Gold Ingot",
        "Cobalt Ore", "Cobalt Ingot",
        "Tin Ore", "Tin Ingot",
        "Zinc Ore", "Zinc Ingot",
        "Pine Wood", "Refined Pine Wood",
        "Cedar Wood", "Refined Cedar Wood",
        "Maple Wood", "Refined Maple Wood",
        "Teak Wood", "Refined Teak Wood",
        "Yew Wood", "Refined Yew Wood",
        "Lean Leather", "Processed Lean Leather",
        "Dense Leather", "Processed Dense Leather",
        "Robust Leather", "Processed Robust Leather",
        "Thick Leather", "Processed Thick Leather",
        "Tough Leather", "Processed Tough Leather",
        "Linen Fiber", "Linen Cloth",
        "Cotton Fiber", "Cotton Cloth",
        "Ramie Fiber", "Ramie Cloth",
        "Jute Fiber", "Jute Cloth",
        "Hemp Fiber", "Hemp Cloth",
        "Viscous Ember Flux", "Refined Ember Flux",
        "Thick Ember Flux"
    };

    // Returns true if the AuctionRecord's CachedItemName matches a material name
    public static bool IsMaterial(object auctionRecord)
    {
        if (auctionRecord == null) return false;
        var prop = auctionRecord.GetType().GetProperty("CachedItemName");
        var displayName = prop?.GetValue(auctionRecord, null) as string;
        return !string.IsNullOrEmpty(displayName) && MaterialNames.Contains(displayName);
    }
}