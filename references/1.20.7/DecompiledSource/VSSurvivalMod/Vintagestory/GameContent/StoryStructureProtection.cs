using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class StoryStructureProtection : ModSystem
{
	private ICoreAPI api;

	private StoryStructuresSpawnConditions ssys;

	public override bool ShouldLoad(ICoreAPI api)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
		api.Event.OnTestBlockAccess += Event_OnTestBlockAccess;
		ssys = api.ModLoader.GetModSystem<StoryStructuresSpawnConditions>();
	}

	private EnumWorldAccessResponse Event_OnTestBlockAccess(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, ref string claimant, EnumWorldAccessResponse response)
	{
		if (accessType == EnumBlockAccessFlags.Use && response == EnumWorldAccessResponse.Granted)
		{
			if (blockSel.Position.dimension != 0)
			{
				return response;
			}
			IBlockAccessor ba = api.World.BlockAccessor;
			GeneratedStructure struc = ssys.GetStoryStructureAt(blockSel.Position);
			if (struc == null)
			{
				return response;
			}
			if (struc.Code == "village:game:story/village" || struc.Code == "tobiascave:game:story/tobiascave")
			{
				if (player.CurrentEntitySelection != null && player.CurrentEntitySelection.Entity is EntityArmorStand)
				{
					claimant = ((struc.Code == "tobiascave:game:story/tobiascave") ? "custommessage-tobias" : "custommessage-nadiya");
					return EnumWorldAccessResponse.NoPrivilege;
				}
				Block block = ba.GetBlock(blockSel.Position);
				if (block.GetBEBehavior<BEBehaviorDoor>(blockSel.Position) != null || block.GetBEBehavior<BEBehaviorTrapDoor>(blockSel.Position) != null)
				{
					return response;
				}
				BlockEntity be = ba.GetBlockEntity(blockSel.Position);
				if (be == null || be is BlockEntityMicroBlock)
				{
					return response;
				}
				if (be is BlockEntityGenericTypedContainer { retrieveOnly: not false })
				{
					return response;
				}
				claimant = ((struc.Code == "tobiascave:game:story/tobiascave") ? "custommessage-tobias" : "custommessage-nadiya");
				return EnumWorldAccessResponse.NoPrivilege;
			}
		}
		return response;
	}
}
