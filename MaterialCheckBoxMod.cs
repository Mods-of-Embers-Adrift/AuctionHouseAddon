using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

// Game namespaces
using SoL.Game;
using SoL.Game.AuctionHouse;

public class MaterialCheckBoxMod : MelonMod
{
    internal static MelonPreferences_Category prefsCategory;
    internal static MelonPreferences_Entry<bool> materialCheckboxValue;

    public override void OnInitializeMelon()
    {
        prefsCategory = MelonPreferences.CreateCategory("MaterialCheckBoxMod", "Auction House Mod Settings");
        materialCheckboxValue = prefsCategory.CreateEntry("MaterialCheckbox", false, "Material Checkbox State");
    }
}

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
            // Only adjust if necessary. If a HorizontalLayoutGroup is present, you can skip this.
            var parentLayout = pinnedToggle.transform.parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (parentLayout == null)
            {
                // Move to the right of the Pinned toggle
                rt.anchoredPosition = pinnedToggle.GetComponent<RectTransform>().anchoredPosition +
                                     new Vector2(rt.sizeDelta.x + 20f, 0);
            }
        }

        // Set persistent value
        newToggle.isOn = MaterialCheckBoxMod.materialCheckboxValue.Value;
        newToggle.onValueChanged.RemoveAllListeners();
        newToggle.onValueChanged.AddListener((value) =>
        {
            MaterialCheckBoxMod.materialCheckboxValue.Value = value;
            MelonLogger.Msg($"Material Checkbox toggled: {value}");
            // Add custom logic here (for filtering, etc.)
        });

        MelonLogger.Msg("Material Checkbox injected to the right of Pinned!");
    }
}