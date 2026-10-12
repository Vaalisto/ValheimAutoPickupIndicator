using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace AutoPickupHUD
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class AutoPickupHUDPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.user.autopickuphud";
        public const string PluginName = "Auto Pickup HUD Indicator";
        public const string PluginVersion = "1.0.0";

        private static TextMeshProUGUI hudText;
        private static bool isInitialized = false;
        private readonly Harmony harmony = new Harmony(PluginGUID);

        // Correct static field reference syntax for HarmonyLib
        private static readonly AccessTools.FieldRef<bool> GetEnableAutoPickup =
            AccessTools.StaticFieldRefAccess<bool>(AccessTools.Field(typeof(Player), "m_enableAutoPickup"));

        private void Awake()
        {
            harmony.PatchAll();
            Logger.LogInfo($"{PluginName} loaded successfully!");
        }

        [HarmonyPatch(typeof(Hud), "Awake")]
        public static class Hud_Awake_Patch
        {
            public static void Postfix()
            {
                isInitialized = false;
                hudText = null;
            }
        }

        [HarmonyPatch(typeof(Hud), "Update")]
        public static class Hud_Update_Patch
        {
            public static void Postfix(Hud __instance)
            {
                if (Player.m_localPlayer == null) return;

                if (!isInitialized)
                {
                    InitializeHUD(__instance);
                    isInitialized = true;
                }

                if (hudText == null) return;

                if (!Hud.IsUserHidden())
                {
                    hudText.gameObject.SetActive(true);

                    bool autoPickupEnabled = GetEnableAutoPickup();

                    if (autoPickupEnabled)
                    {
                        hudText.text = "Auto Pickup: <color=#55FF55>ON</color>";
                    }
                    else
                    {
                        hudText.text = "Auto Pickup: <color=#FF5555>OFF</color>";
                    }
                }
                else
                {
                    hudText.gameObject.SetActive(false);
                }
            }

            private static void InitializeHUD(Hud hud)
            {
                if (hud.m_rootObject == null) return;

                GameObject textObj = new GameObject("AutoPickupHUD_Text");
                textObj.transform.SetParent(hud.m_rootObject.transform, false);

                RectTransform rectTransform = textObj.AddComponent<RectTransform>();

                rectTransform.anchorMin = new Vector2(1f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(1f, 1f);
                
                rectTransform.anchoredPosition = new Vector2(-50f, -250f);
                rectTransform.sizeDelta = new Vector2(300f, 60f);

                hudText = textObj.AddComponent<TextMeshProUGUI>();

                if (hud.m_healthText != null && hud.m_healthText.font != null)
                {
                    hudText.font = hud.m_healthText.font;
                    hudText.fontSharedMaterial = hud.m_healthText.fontSharedMaterial;
                }
                else
                {
                    TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    if (fonts.Length > 0)
                    {
                        hudText.font = fonts[0];
                    }
                }

                hudText.fontSize = 22;
                hudText.alignment = TextAlignmentOptions.TopRight;
                hudText.color = Color.white;
                hudText.raycastTarget = false;
                hudText.textWrappingMode = TextWrappingModes.NoWrap;
                hudText.outlineWidth = 0.25f;
                hudText.outlineColor = Color.black;

                Debug.Log("[AutoPickupHUD] HUD text element initialized successfully!");
            }
        }
    }
}