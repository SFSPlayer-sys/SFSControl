using ModLoader;
using UnityEngine;
using SFSControl;
using HarmonyLib;

namespace SFSControl
{
    public static class DeltaVPatchHelper
    {
        public static double? LastTransferWindowDeltaV = null;
    }

    public class Main : ModLoader.Mod
    {
        public override string ModNameID => "SFSControl";
        public override string DisplayName => "SFSControl";
        public override string Author => "SFSGamer"; 
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string ModVersion => "1.0.7";
        public override string Description => "Provide an interface for scripts to control SFS externally.";

        private Server serverComponent;

        public override void Load()
        {
            var settings = SettingsManager.LoadSettings();

            var serverObject = new GameObject("SFSControl_Instance");
            GameObject.DontDestroyOnLoad(serverObject);
            serverComponent = serverObject.AddComponent<Server>();
            serverComponent.StartServer(settings.port);

            Application.runInBackground = true;
            new Harmony("SFSControl.DeltaVPatch").PatchAll();
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
}