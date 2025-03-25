using System;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using System.Linq;
using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace InvertHotbarScroll
{
    public class InvertHotbarScrollModSystem: ModSystem
    {
        ICoreClientAPI capi;
        private Harmony patcher;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            if (api.Side == EnumAppSide.Client &&
                !Harmony.HasAnyPatches(Mod.Info.ModID))
            {
                capi = api as ICoreClientAPI;
                patcher = new Harmony(Mod.Info.ModID);
                patcher.PatchCategory(Mod.Info.ModID);
                capi.Logger.Debug($"{Mod.Info.ModID} patches applied");
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            patcher?.UnpatchAll(Mod.Info.ModID);
        }
    }

    [HarmonyPatchCategory("inverthotbarscroll")]
    internal static class Patches
    {
        [HarmonyPrefix()]
        [HarmonyPatch(typeof(HudHotbar), "OnMouseWheel")]
        public static bool BeforeHudHotbarOnMouseWheel(
            HudHotbar __instance, ref MouseWheelEventArgs args)
        {
            // Invert the delta
            args.delta *= -1;
            args.deltaPrecise *= -1;

            // Execute original method with new args
            return true;
        }
    }
}
