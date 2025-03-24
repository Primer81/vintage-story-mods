using System;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace HotbarScrollControl
{
    public class HotbarScrollControlModSystem: ModSystem
    {
        private ICoreClientAPI capi = null;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            if (api.Side == EnumAppSide.Client)
            {
                capi = api as ICoreClientAPI;

                // Register for active slot change events
                // with the correct parameter order
                capi.Event.MouseUp += OnMouseUp;
                capi.Event.MouseDown += OnMouseDown;
                capi.Event.MouseMove += OnMouseMove;
            }
        }

        public override void Dispose()
        {
            if (capi != null)
            {
                capi.Event.MouseUp -= OnMouseUp;
                capi.Event.MouseDown -= OnMouseDown;
                capi.Event.MouseMove -= OnMouseMove;
            }
            base.Dispose();
        }

        private void OnMouseUp(MouseEvent args)
        {
            // Code to execute when mouse button is pressed
            capi.ShowChatMessage($"Mouse up at: {args.X}, {args.Y}, Button: {args.Button}");
        }

        private void OnMouseDown(MouseEvent args)
        {
            // Code to execute when mouse button is pressed
            capi.ShowChatMessage($"Mouse down at: {args.X}, {args.Y}, Button: {args.Button}");
        }

        private void OnMouseMove(MouseEvent args)
        {
            capi.ShowChatMessage($"Mouse move at: {args.X}, {args.Y}, Button: {args.Button}");
        }
    }
}
