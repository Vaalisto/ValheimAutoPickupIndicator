using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace AutoPickupHUD
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public class AutoPickupHUDPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.user.autopickuphud";
        public const string PluginName = "Auto Pickup HUD Indicator";

        // BepInPlugin needs a compile-time constant, so this has to stay in sync with
        // <Version> in AutoPickupHUD.csproj by hand.
        public const string PluginVersion = "1.0.0";

        private const string OnText = "Auto Pickup: <color=#55FF55>ON</color>";
        private const string OffText = "Auto Pickup: <color=#FF5555>OFF</color>";

        private static ManualLogSource Log;
        private static ConfigEntry<bool> ShowIndicator;

        private static RectTransform indicator;
        private static TMP_Text indicatorLabel;
        private static string lastText;
        private static bool? lastVisible;
        private static float lastRowY = float.NaN;

        private static AccessTools.FieldRef<bool> enableAutoPickup;
        private static bool autoPickupLookupFailed;

        private readonly Harmony harmony = new Harmony(PluginGUID);

        private void Awake()
        {
            Log = Logger;

            ShowIndicator = Config.Bind("General", "ShowIndicator", true,
                "Show the auto pickup indicator with the HUD status effects.");

            harmony.PatchAll();
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded successfully!");
        }

        /// <summary>
        /// Reads Player.m_enableAutoPickup (private static). Resolved lazily and cached, so a
        /// renamed field in a future game update degrades into one logged error instead of a
        /// TypeInitializationException thrown from the patch on every frame.
        /// </summary>
        private static bool TryGetAutoPickup(out bool enabled)
        {
            enabled = false;
            if (autoPickupLookupFailed) return false;

            if (enableAutoPickup == null)
            {
                FieldInfo field = AccessTools.Field(typeof(Player), "m_enableAutoPickup");
                if (field == null)
                {
                    autoPickupLookupFailed = true;
                    Log?.LogError("Player.m_enableAutoPickup was not found. Hiding the indicator; " +
                                  "a game update probably renamed the field.");
                    return false;
                }

                enableAutoPickup = AccessTools.StaticFieldRefAccess<bool>(field);
            }

            enabled = enableAutoPickup();
            return true;
        }

        /// <summary>
        /// Clones the vanilla status effect entry so the label uses the same font, size,
        /// outline and animator as "Fire" / "Wisplight", then strips the icon and timer slots.
        /// </summary>
        private static bool TryInitializeHud(Hud hud)
        {
            if (hud == null || hud.m_statusEffectListRoot == null || hud.m_statusEffectTemplate == null)
                return false;

            RectTransform clone = Object.Instantiate(hud.m_statusEffectTemplate, hud.m_statusEffectListRoot);
            clone.name = "AutoPickupHUD_StatusEffect";
            clone.gameObject.SetActive(true);

            // Vanilla's Hud.UpdateStatusEffects finds the name label exactly like this, so this
            // is guaranteed to be the same text object the real status effects use.
            TMP_Text label = clone.GetComponentInChildren<TMP_Text>();
            if (label == null)
            {
                Object.Destroy(clone.gameObject);
                Log?.LogWarning("Status effect template has no TMP_Text label; indicator not created.");
                return false;
            }

            HideSlot(clone, "Icon", label);
            HideSlot(clone, "Cooldown", label);
            HideSlot(clone, "TimeText", label);

            // "No skill drain" is already wider than its cell, so make sure our longer label
            // is never clipped.
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.richText = true;
            label.raycastTarget = false;

            indicator = clone;
            indicatorLabel = label;
            lastText = null;
            lastRowY = float.NaN;

            Log?.LogDebug("Status effect style indicator created.");
            return true;
        }

        private static void HideSlot(RectTransform entry, string childName, TMP_Text label)
        {
            Transform child = entry.Find(childName);
            if (child == null) return;

            // Never hide the object the label itself lives under.
            if (label != null && label.transform.IsChildOf(child)) return;

            child.gameObject.SetActive(false);
        }

        /// <summary>
        /// Vanilla lays its entries out from the top-right corner, one row per
        /// m_effectsPerRow effects. Sit on the row below the last one so we never overlap.
        /// </summary>
        private static void UpdatePlacement(Hud hud)
        {
            if (indicator == null) return;

            float spacing = hud.m_statusEffectSpacing > 0f ? hud.m_statusEffectSpacing : 55f;
            int perRow = Mathf.Max(1, hud.m_effectsPerRow);
            int rows = Mathf.CeilToInt(CountVanillaEntries(hud) / (float)perRow);
            float y = -rows * spacing;

            if (Mathf.Approximately(y, lastRowY)) return;

            lastRowY = y;
            indicator.anchoredPosition = new Vector3(-4f, y, 0f);
        }

        private static int CountVanillaEntries(Hud hud)
        {
            Transform root = hud.m_statusEffectListRoot;
            int count = 0;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (indicator != null && child == indicator.transform) continue;
                if (child == hud.m_statusEffectTemplate) continue;
                if (!child.gameObject.activeSelf) continue;

                count++;
            }

            return count;
        }

        private static void SetVisible(bool visible)
        {
            if (indicator == null || lastVisible == visible) return;

            indicator.gameObject.SetActive(visible);
            lastVisible = visible;
        }

        [HarmonyPatch(typeof(Hud), "Awake")]
        public static class Hud_Awake_Patch
        {
            public static void Postfix()
            {
                // A new Hud means the previous clone was destroyed with the old one.
                indicator = null;
                indicatorLabel = null;
                lastText = null;
                lastVisible = null;
                lastRowY = float.NaN;
            }
        }

        [HarmonyPatch(typeof(Hud), "Update")]
        public static class Hud_Update_Patch
        {
            public static void Postfix(Hud __instance)
            {
                if (Player.m_localPlayer == null)
                {
                    SetVisible(false);
                    return;
                }

                if (indicator == null && !TryInitializeHud(__instance)) return;

                if (!TryGetAutoPickup(out bool enabled))
                {
                    SetVisible(false);
                    return;
                }

                bool visible = (ShowIndicator?.Value ?? true) && !Hud.IsUserHidden();
                SetVisible(visible);
                if (!visible) return;

                UpdatePlacement(__instance);

                // Assigning .text forces a TMP re-parse and mesh rebuild, so only do it on change.
                string text = enabled ? OnText : OffText;
                if (text != lastText)
                {
                    indicatorLabel.text = text;
                    lastText = text;
                }
            }
        }
    }
}
