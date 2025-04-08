using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf;

internal class HudDropItem : HudElement
{
	public override double DrawOrder => 1.0;

	public override bool Focusable => false;

	public HudDropItem(ICoreClientAPI capi)
		: base(capi)
	{
		TryOpen();
	}

	public override bool TryClose()
	{
		return false;
	}

	public override void OnMouseDown(MouseEvent args)
	{
		if (args.Handled)
		{
			return;
		}
		foreach (GuiDialog guiDialog in capi.Gui.OpenedGuis)
		{
			if (!guiDialog.IsOpened() || guiDialog is HudMouseTools)
			{
				continue;
			}
			foreach (GuiComposer value in guiDialog.Composers.Values)
			{
				if (value.Bounds.PointInside(args.X, args.Y))
				{
					return;
				}
			}
		}
		if (capi.World.Player.InventoryManager.DropMouseSlotItems(args.Button == EnumMouseButton.Left))
		{
			args.Handled = true;
		}
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		return true;
	}
}
