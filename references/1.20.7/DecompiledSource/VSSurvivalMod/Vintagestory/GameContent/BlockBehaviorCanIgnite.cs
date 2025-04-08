using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBehaviorCanIgnite : BlockBehavior
{
	public static List<ItemStack> CanIgniteStacks(ICoreAPI api, bool withFirestarter)
	{
		List<ItemStack> canIgniteStacks = ObjectCacheUtil.GetOrCreate(api, "canIgniteStacks", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			List<ItemStack> canIgniteStacksWithFirestarter2 = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				if (current is Block block)
				{
					if (block.HasBehavior<BlockBehaviorCanIgnite>())
					{
						List<ItemStack> handBookStacks = current.GetHandBookStacks(api as ICoreClientAPI);
						if (handBookStacks != null)
						{
							list.AddRange(handBookStacks);
							canIgniteStacksWithFirestarter2.AddRange(handBookStacks);
						}
					}
				}
				else if (current is ItemFirestarter)
				{
					List<ItemStack> handBookStacks2 = current.GetHandBookStacks(api as ICoreClientAPI);
					canIgniteStacksWithFirestarter2.AddRange(handBookStacks2);
				}
			}
			ObjectCacheUtil.GetOrCreate(api, "canIgniteStacksWithFirestarter", () => canIgniteStacksWithFirestarter2);
			return list;
		});
		List<ItemStack> canIgniteStacksWithFirestarter = ObjectCacheUtil.GetOrCreate(api, "canIgniteStacksWithFirestarter", () => new List<ItemStack>());
		if (!withFirestarter)
		{
			return canIgniteStacks;
		}
		return canIgniteStacksWithFirestarter;
	}

	public BlockBehaviorCanIgnite(Block block)
		: base(block)
	{
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling blockHandling)
	{
		if (blockSel == null)
		{
			return;
		}
		Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			EnumIgniteState state = EnumIgniteState.NotIgnitable;
			IIgnitable ign = block.GetInterface<IIgnitable>(byEntity.World, blockSel.Position);
			if (ign != null)
			{
				state = ign.OnTryIgniteBlock(byEntity, blockSel.Position, 0f);
			}
			if (state == EnumIgniteState.NotIgnitablePreventDefault)
			{
				blockHandling = EnumHandling.PreventDefault;
				handHandling = EnumHandHandling.PreventDefault;
			}
			if (byEntity.Controls.ShiftKey || state == EnumIgniteState.Ignitable)
			{
				blockHandling = EnumHandling.PreventDefault;
				handHandling = EnumHandHandling.PreventDefault;
				byEntity.World.PlaySoundAt(new AssetLocation("sounds/torch-ignite"), byEntity, byPlayer, randomizePitch: false, 16f);
			}
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		if (blockSel == null)
		{
			return false;
		}
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		Block obj = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
		EnumIgniteState igniteState = EnumIgniteState.NotIgnitable;
		IIgnitable ign = obj.GetInterface<IIgnitable>(byEntity.World, blockSel.Position);
		if (ign != null)
		{
			igniteState = ign.OnTryIgniteBlock(byEntity, blockSel.Position, secondsUsed);
		}
		if (igniteState == EnumIgniteState.NotIgnitablePreventDefault)
		{
			return false;
		}
		handling = EnumHandling.PreventDefault;
		if (byEntity.World is IClientWorldAccessor && secondsUsed > 0.25f && (int)(30f * secondsUsed) % 2 == 1)
		{
			Random rand = byEntity.World.Rand;
			Vec3d pos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition).Add(rand.NextDouble() * 0.25 - 0.125, rand.NextDouble() * 0.25 - 0.125, rand.NextDouble() * 0.25 - 0.125);
			Block blockFire = byEntity.World.GetBlock(new AssetLocation("fire"));
			AdvancedParticleProperties props = blockFire.ParticleProperties[blockFire.ParticleProperties.Length - 1].Clone();
			props.basePos = pos;
			props.Quantity.avg = 0.5f;
			byEntity.World.SpawnParticles(props, byPlayer);
			props.Quantity.avg = 0f;
		}
		if (byEntity.World.Side == EnumAppSide.Server)
		{
			return true;
		}
		return (double)secondsUsed <= 3.2;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		if (blockSel == null || secondsUsed < 3f)
		{
			return;
		}
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return;
		}
		EnumHandling handled = EnumHandling.PassThrough;
		byEntity.World.BlockAccessor.GetBlock(blockSel.Position).GetInterface<IIgnitable>(byEntity.World, blockSel.Position)?.OnTryIgniteBlockOver(byEntity, blockSel.Position, secondsUsed, ref handled);
		if (handled != 0)
		{
			return;
		}
		handling = EnumHandling.PreventDefault;
		if (byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak) && blockSel != null && byEntity.World.Side == EnumAppSide.Server)
		{
			BlockPos bpos = blockSel.Position.AddCopy(blockSel.Face);
			if (byEntity.World.BlockAccessor.GetBlock(bpos).BlockId == 0)
			{
				byEntity.World.BlockAccessor.SetBlock(byEntity.World.GetBlock(new AssetLocation("fire")).BlockId, bpos);
				byEntity.World.BlockAccessor.GetBlockEntity(bpos)?.GetBehavior<BEBehaviorBurning>()?.OnFirePlaced(blockSel.Face, (byEntity as EntityPlayer).PlayerUID);
			}
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				HotKeyCode = "shift",
				ActionLangCode = "heldhelp-igniteblock",
				MouseButton = EnumMouseButton.Right
			}
		};
	}
}
