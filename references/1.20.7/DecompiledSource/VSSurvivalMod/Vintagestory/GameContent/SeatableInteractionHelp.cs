using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class SeatableInteractionHelp
{
	public static WorldInteraction[] GetOrCreateInteractionHelp(ICoreAPI api, EntityBehaviorSeatable eba, IMountableSeat[] seats, int slotIndex)
	{
		IMountableSeat seat = getSeat(eba, seats, slotIndex);
		if (seat == null)
		{
			return null;
		}
		JsonObject attributes = seat.Config.Attributes;
		if (attributes != null && attributes["ropeTieablesOnly"].AsBool())
		{
			List<ItemStack> ropetiableStacks = ObjectCacheUtil.GetOrCreate(api, "interactionhelp-ropetiablestacks", delegate
			{
				List<ItemStack> list = new List<ItemStack>();
				foreach (EntityProperties current in api.World.EntityTypes)
				{
					JsonObject[] behaviorsAsJsonObj = current.Client.BehaviorsAsJsonObj;
					for (int i = 0; i < behaviorsAsJsonObj.Length; i++)
					{
						if (behaviorsAsJsonObj[i]["code"].AsString() == "ropetieable")
						{
							Item item = api.World.GetItem(AssetLocation.Create("creature-" + current.Code.Path, current.Code.Domain));
							if (item != null)
							{
								list.Add(new ItemStack(item));
							}
						}
					}
				}
				return list;
			});
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = ((seat.Passenger != null) ? "seatableentity-dismountcreature" : "seatableentity-mountcreature"),
					Itemstacks = ropetiableStacks.ToArray(),
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		if (eba.CanSitOn(seat))
		{
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "seatableentity-mount",
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		return null;
	}

	private static bool canSit(EntityBehaviorSeatable eba, IMountableSeat[] seats, int slotIndex)
	{
		if (slotIndex >= 0)
		{
			IMountableSeat seat = getSeat(eba, seats, slotIndex);
			if (seat != null && eba.CanSitOn(seat))
			{
				return true;
			}
		}
		return false;
	}

	private static IMountableSeat getSeat(EntityBehaviorSeatable eba, IMountableSeat[] seats, int slotIndex)
	{
		EntityBehaviorSelectionBoxes bhs = eba.entity.GetBehavior<EntityBehaviorSelectionBoxes>();
		if (bhs == null)
		{
			return null;
		}
		AttachmentPointAndPose apap = bhs.selectionBoxes[slotIndex];
		string apname = apap.AttachPoint.Code;
		return seats.FirstOrDefault((IMountableSeat seat) => seat.Config.APName == apname || seat.Config.SelectionBox == apname);
	}
}
