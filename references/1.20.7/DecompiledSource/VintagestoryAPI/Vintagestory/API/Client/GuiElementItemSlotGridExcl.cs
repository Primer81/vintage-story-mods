using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

/// <summary>
/// Builds slot grid with exclusions to the grid.
/// </summary>
public class GuiElementItemSlotGridExcl : GuiElementItemSlotGridBase
{
	private int[] excludingSlots;

	/// <summary>
	/// Creates a new slot grid with exclusions.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="inventory">The attached inventory.</param>
	/// <param name="sendPacketHandler">A handler that should send supplied network packet to the server, if the inventory modifications should be synced</param>
	/// <param name="columns">The number of columns in the slot grid.</param>
	/// <param name="excludingSlots">The slots that have been excluded.</param>
	/// <param name="bounds">The bounds of the slot grid.</param>
	public GuiElementItemSlotGridExcl(ICoreClientAPI capi, IInventory inventory, Action<object> sendPacketHandler, int columns, int[] excludingSlots, ElementBounds bounds)
		: base(capi, inventory, sendPacketHandler, columns, bounds)
	{
		this.excludingSlots = excludingSlots;
		InitDicts();
		SendPacketHandler = sendPacketHandler;
	}

	internal void InitDicts()
	{
		availableSlots.Clear();
		renderedSlots.Clear();
		if (excludingSlots != null)
		{
			for (int i = 0; i < inventory.Count; i++)
			{
				if (!excludingSlots.Contains(i))
				{
					ItemSlot slot = inventory[i];
					availableSlots.Add(i, slot);
					renderedSlots.Add(i, slot);
				}
			}
		}
		else
		{
			for (int j = 0; j < inventory.Count; j++)
			{
				availableSlots.Add(j, inventory[j]);
				renderedSlots.Add(j, inventory[j]);
			}
		}
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		InitDicts();
		base.ComposeElements(ctx, surface);
	}

	public override void PostRenderInteractiveElements(float deltaTime)
	{
		if (inventory.DirtySlots.Count > 0)
		{
			InitDicts();
		}
		base.PostRenderInteractiveElements(deltaTime);
	}
}
