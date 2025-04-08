using System;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

/// <summary>
/// Displays the slots of an inventory in the form of a slot grid
/// </summary>
public class GuiElementItemSlotGrid : GuiElementItemSlotGridBase
{
	public GuiElementItemSlotGrid(ICoreClientAPI capi, IInventory inventory, Action<object> SendPacketHandler, int cols, int[] visibleSlots, ElementBounds bounds)
		: base(capi, inventory, SendPacketHandler, cols, bounds)
	{
		DetermineAvailableSlots(visibleSlots);
		base.SendPacketHandler = SendPacketHandler;
	}

	/// <summary>
	/// Determines the available slots for the slot grid.
	/// </summary>
	/// <param name="visibleSlots"></param>
	public void DetermineAvailableSlots(int[] visibleSlots = null)
	{
		availableSlots.Clear();
		renderedSlots.Clear();
		if (visibleSlots != null)
		{
			for (int i = 0; i < visibleSlots.Length; i++)
			{
				availableSlots.Add(visibleSlots[i], inventory[visibleSlots[i]]);
				renderedSlots.Add(visibleSlots[i], inventory[visibleSlots[i]]);
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
}
