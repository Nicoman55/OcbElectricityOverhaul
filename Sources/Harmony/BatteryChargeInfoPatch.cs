using HarmonyLib;
using UnityEngine;

// ####################################################################
// Shows a dynamic charge percentage in the item description of car
// batteries (the battery bank slot item). Since this mod repurposes
// `UseTimes` as the charge level (0 = full, MaxUseTimes = empty),
// the vanilla durability display doesn't tell the whole story.
// A second patch periodically refreshes the info window bindings
// so the percentage updates live while charging or discharging.
// ####################################################################

public class BatteryChargeInfoPatch
{

    // Item name of the battery bank slot item (see blocks.xml SlotItem)
    private const string BatteryItemName = "carBattery";

    // ####################################################################
    // ####################################################################

    // Append the charge line to the item description binding
    [HarmonyPatch(typeof(XUiC_ItemInfoWindow))]
    [HarmonyPatch("GetBindingValueInternal")]
    public class XUiC_ItemInfoWindow_GetBindingValueInternal
    {
        static void Postfix(ref string value, string bindingName,
            ItemStack ___itemStack, ref bool __result)
        {
            if (bindingName != "itemdescription") return;
            if (___itemStack == null || ___itemStack.IsEmpty()) return;
            ItemValue iv = ___itemStack.itemValue;
            if (iv?.ItemClass == null) return;
            if (iv.ItemClass.Name != BatteryItemName) return;
            if (iv.MaxUseTimes <= 0) return;
            float charge = 100f * (1f - iv.UseTimes / iv.MaxUseTimes);
            charge = Mathf.Clamp(charge, 0f, 100f);
            value = string.Format("{0}\n\nCharge: {1:0.0} %", value, charge);
            __result = true;
        }
    }

    // ####################################################################
    // ####################################################################

    // Periodically refresh the info window bindings while it displays
    // a battery, so the charge percentage updates while looking at it
    [HarmonyPatch(typeof(XUiC_ItemInfoWindow))]
    [HarmonyPatch("Update")]
    public class XUiC_ItemInfoWindow_Update
    {
        static float lastRefresh;
        static void Postfix(XUiC_ItemInfoWindow __instance,
            ItemStack ___itemStack)
        {
            if (__instance.ViewComponent == null) return;
            if (!__instance.ViewComponent.IsVisible) return;
            if (___itemStack == null || ___itemStack.IsEmpty()) return;
            ItemValue iv = ___itemStack.itemValue;
            if (iv?.ItemClass == null) return;
            if (iv.ItemClass.Name != BatteryItemName) return;
            // Throttle refreshes to twice per second
            if (Time.time < lastRefresh + 0.5f) return;
            lastRefresh = Time.time;
            __instance.RefreshBindings();
        }
    }

    // ####################################################################
    // ####################################################################

}
