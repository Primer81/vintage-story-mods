using System;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf.HudHotbar;
using System.Linq;

namespace HotbarScrollControl
{
    // public class CustomHudHotbar: HudHotbar
    // {
    //     private ICoreClientAPI capi;

    //     // Constructor - we need to pass the client API
    //     public CustomHudHotbar(ICoreClientAPI capi): base(capi)
    //     {
    //         this.capi = capi;
    //     }

    //     // Override the OnMouseWheel method
    //     public override bool OnMouseWheel(float deltaZ)
    //     {
    //         // Log the interception
    //         capi.Logger.Debug($"Custom HudHotbar intercepted mouse wheel: {deltaZ}");

    //         // Call the original implementation
    //         return base.OnMouseWheel(deltaZ);
    //     }
    // }

    public class HotbarScrollControlModSystem: ModSystem
    {
        ICoreClientAPI capi;
        // private HudHotbar originalHotbar;
        // private CustomHudHotbar customHotbar;
        private long callbackId; // Store the callback ID for later removal

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            if (api.Side == EnumAppSide.Client)
            {
                capi = api as ICoreClientAPI;
                // Register callback and store the returned ID
                callbackId = capi.Event.RegisterCallback(
                    dt => ModifyHotbarGui(), 1000);
            }
        }

        public override void Dispose()
        {
            // Restore original hotbar if needed
            // if (capi != null && customHotbar != null)
            // {
            //     capi.Gui.LoadedGuis.Remove(customHotbar);
            //     customHotbar.TryClose();

            //     if (originalHotbar != null)
            //     {
            //         capi.Gui.LoadedGuis.Add(originalHotbar);
            //         originalHotbar.TryOpen();
            //     }
            // }

            // Unregister the callback
            if (capi != null)
            {
                capi.Event.UnregisterCallback(callbackId);
            }

            base.Dispose();
        }

        private void ModifyHotbarGui()
        {
            // Find the hotbar GUI
            // originalHotbar = null;
            // foreach (var gui in capi.Gui.LoadedGuis)
            // {
            //     if (gui.GetType() == typeof(HudHotbar))
            //     {
            //         originalHotbar = gui as HudHotbar;
            //         break;
            //     }
            // }
            GuiDialog originalHotbar = capi.Gui.LoadedGuis.FirstOrDefault(gui =>
                gui.DebugName == "HudHotbar");

            // if (originalHotbar != null)
            // {
            //     // Create your custom hotbar
            //     customHotbar = new CustomHudHotbar(capi);

            //     // Remove the original from loaded GUIs
            //     capi.Gui.LoadedGuis.Remove(originalHotbar);

            //     // Close the original
            //     originalHotbar.TryClose();

            //     // Add custom hotbar to loaded GUIs
            //     capi.Gui.LoadedGuis.Add(customHotbar);

            //     // Open the custom hotbar
            //     customHotbar.TryOpen();

            //     capi.Logger.Debug("Successfully replaced HudHotbar with CustomHudHotbar");
            // }
            // else
            // {
            //     capi.Logger.Error("Could not find HudHotbar to replace");
            // }
        }
    }
}
