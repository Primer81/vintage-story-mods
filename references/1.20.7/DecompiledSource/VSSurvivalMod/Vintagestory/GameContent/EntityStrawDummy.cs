using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityStrawDummy : EntityHumanoid
{
	public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
	{
		if (!Alive || World.Side == EnumAppSide.Client || mode == EnumInteractMode.Attack)
		{
			base.OnInteract(byEntity, slot, hitPosition, mode);
			return;
		}
		string owneruid = WatchedAttributes.GetString("ownerUid");
		string agentUid = (byEntity as EntityPlayer)?.PlayerUID;
		if (agentUid != null && (owneruid == null || owneruid == "" || owneruid == agentUid) && byEntity.Controls.ShiftKey)
		{
			ItemStack stack = new ItemStack(byEntity.World.GetItem(new AssetLocation("strawdummy")));
			if (!byEntity.TryGiveItemStack(stack))
			{
				byEntity.World.SpawnItemEntity(stack, ServerPos.XYZ);
			}
			byEntity.World.Logger.Audit("{0} Took 1x{1} at {2}.", byEntity.GetName(), stack.Collectible.Code, ServerPos.AsBlockPos);
			Die();
		}
		else
		{
			base.OnInteract(byEntity, slot, hitPosition, mode);
		}
	}
}
