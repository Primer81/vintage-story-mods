using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ItemNpcGuideStick : Item
{
	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		handling = EnumHandHandling.PreventDefault;
		if (blockSel != null)
		{
			Vec3d pos = blockSel.FullPosition;
			(api as ICoreServerAPI)?.ChatCommands.ExecuteUnparsed($"/npc exec nav ={pos.X} ={pos.Y} ={pos.Z} run 0.006 1.8", new TextCommandCallingArgs
			{
				Caller = new Caller
				{
					Player = (byEntity as EntityPlayer).Player
				}
			});
		}
	}
}
