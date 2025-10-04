using ModLoader;
using UnityEngine;
using SFSControl;
using HarmonyLib;
using System.Collections.Generic;

namespace SFSControl
{
    public static class DeltaVPatchHelper
    {
        public static double? LastTransferWindowDeltaV = null;
    }

    public class Main : ModLoader.Mod
    {
        public const string VERSION = "1.2";
        public override string ModNameID => "SFSControl";
        public override string DisplayName => "SFSControl";
        public override string Author => "SFSGamer"; 
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string ModVersion => VERSION;
        public override string Description => "Add APIs that can be accessed by external programs for SFS.";

        private Server serverComponent;

        public override void Load()
        {
            // 加载设置
            SettingsManager.Load();
            var serverObject = new GameObject("SFSControl_Instance");
            GameObject.DontDestroyOnLoad(serverObject);
            serverComponent = serverObject.AddComponent<Server>();
            serverComponent.StartServer(SettingsManager.settings.port);
            Application.runInBackground = true;
            new Harmony("SFSControl.DeltaVPatch").PatchAll();

			// Ensure Draw system exists so /draw works immediately
			DrawManager.Ensure();

        }
    }

    [HarmonyPatch(typeof(SFS.World.Maps.MapNavigation), "DrawNavigation")]
    public static class Patch_MapNavigation_DrawNavigation
    {
        static void Postfix(SFS.World.Maps.MapNavigation __instance)
        {
            var window = __instance.window;
            bool hasWindow = window.Item1;
            var windowLoc = window.Item2;
            if (hasWindow && windowLoc != null)
            {
                var player = SFS.World.PlayerController.main?.player?.Value;
                if (player != null && player.mapPlayer != null)
                {
                    var currentOrbit = SFS.World.Orbit.TryCreateOrbit(player.mapPlayer.Location, true, false, out bool orbitSuccess);
                    if (currentOrbit != null && orbitSuccess)
                    {
                        var nowLoc = currentOrbit.GetLocation(SFS.World.WorldTime.main.worldTime);
                        double deltaV = windowLoc.velocity.magnitude - nowLoc.velocity.magnitude;
                        DeltaVPatchHelper.LastTransferWindowDeltaV = deltaV;
                        return;
                    }
                }
            }
            DeltaVPatchHelper.LastTransferWindowDeltaV = null;
        }
    }

    // 修复地图图标缩放后变白的问题
    [HarmonyPatch(typeof(SFS.World.Maps.MapIcon), "UpdateAlpha")]
    public static class Patch_MapIcon_UpdateAlpha
    {
        // 存储用户设置的颜色（包括透明度）
        private static Dictionary<SpriteRenderer, Color> userSetColors = new Dictionary<SpriteRenderer, Color>();
        // 标记是否使用用户设置的透明度
        private static Dictionary<SpriteRenderer, bool> useUserAlpha = new Dictionary<SpriteRenderer, bool>();

        // 设置用户颜色
        public static void SetUserColor(SpriteRenderer spriteRenderer, Color color, bool preserveAlpha = false)
        {
            if (spriteRenderer != null)
            {
                userSetColors[spriteRenderer] = color;
                useUserAlpha[spriteRenderer] = preserveAlpha;
            }
        }

        // 清除用户颜色
        public static void ClearUserColor(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer != null)
            {
                userSetColors.Remove(spriteRenderer);
                useUserAlpha.Remove(spriteRenderer);
            }
        }

        static void Postfix(SFS.World.Maps.MapIcon __instance)
        {
            if (__instance.mapIcon == null) return;

            var spriteRenderer = __instance.mapIcon.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null) return;

            // 检查是否有用户设置的颜色
            if (userSetColors.TryGetValue(spriteRenderer, out Color userColor))
            {
                if (__instance.location.planet.Value == null) return;
                
                // 检查是否使用用户设置的透明度
                bool preserveUserAlpha = useUserAlpha.TryGetValue(spriteRenderer, out bool preserve) && preserve;
                
                if (preserveUserAlpha)
                {
                    // 完全使用用户设置的颜色，包括透明度
                    spriteRenderer.color = userColor;
                }
                else
                {
                    // 计算透明度
                    double num = __instance.location.position.Value.magnitude * 50.0 + __instance.location.planet.Value.SOI * 5.0;
                    float fadeOut = SFS.World.Maps.MapDrawer.GetFadeOut(SFS.World.Maps.Map.view.view.distance, num, num * 1.25);
                    
                    // 保持用户设置的RGB颜色，只更新透明度
                    spriteRenderer.color = new Color(userColor.r, userColor.g, userColor.b, fadeOut);
                }
            }
        }
    }

    // 确保SFS在失去焦点时继续运行
    [HarmonyPatch(typeof(UnityEngine.Application), "isFocused", MethodType.Getter)]
    public static class Patch_Application_isFocused
    {
        static bool Postfix(bool __result)
        {
            return true;
        }
    }
    [HarmonyPatch(typeof(UnityEngine.Application), "isPlaying", MethodType.Getter)]
    public static class Patch_Application_isPlaying
    {
        static bool Postfix(bool __result)
        {
            return true;
        }
    }
}