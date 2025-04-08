using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemOpenedBeenade : Item
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "openedBeenadeInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Block current in api.World.Blocks)
			{
				if (!(current.Code == null) && current is BlockSkep && current.FirstCodePart(1).Equals("populated"))
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-fill",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel != null && byEntity.World.Claims.TryAccess((byEntity as EntityPlayer)?.Player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			if (block is BlockSkep && block.FirstCodePart(1).Equals("populated"))
			{
				handling = EnumHandHandling.PreventDefaultAction;
			}
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null)
		{
			return false;
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform tf = new ModelTransform();
			tf.EnsureDefaultValues();
			float offset = GameMath.Clamp(secondsUsed * 3f, 0f, 2f);
			tf.Translation.Set(0f - offset, offset / 4f, 0f);
			byEntity.Controls.UsingHeldItemTransformBefore = tf;
		}
		SimpleParticleProperties bees = BlockEntityBeehive.Bees;
		BlockPos pos = blockSel.Position;
		Random rand = byEntity.World.Rand;
		Vec3d startPos = new Vec3d((double)pos.X + rand.NextDouble(), (double)pos.Y + rand.NextDouble() * 0.25, (double)pos.Z + rand.NextDouble());
		Vec3d endPos = new Vec3d(byEntity.SidedPos.X, byEntity.SidedPos.Y + byEntity.LocalEyePos.Y - 0.20000000298023224, byEntity.SidedPos.Z);
		Vec3f minVelo = new Vec3f((float)(endPos.X - startPos.X), (float)(endPos.Y - startPos.Y), (float)(endPos.Z - startPos.Z));
		minVelo.Normalize();
		minVelo *= 2f;
		bees.MinPos = startPos;
		bees.MinVelocity = minVelo;
		bees.WithTerrainCollision = true;
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		byEntity.World.SpawnParticles(bees, byPlayer);
		return secondsUsed < 4f;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null || !byEntity.World.Claims.TryAccess((byEntity as EntityPlayer)?.Player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			return;
		}
		Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
		if (block is BlockSkep && block.FirstCodePart(1).Equals("populated") && !(secondsUsed < 3.9f))
		{
			slot.TakeOut(1);
			slot.MarkDirty();
			IPlayer byPlayer = null;
			if (byEntity is EntityPlayer)
			{
				byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			byPlayer?.InventoryManager.TryGiveItemstack(new ItemStack(byEntity.World.GetItem(new AssetLocation("beenade-closed"))));
			Block skepemtpyblock = byEntity.World.GetBlock(new AssetLocation("skep-empty-" + block.LastCodePart()));
			byEntity.World.BlockAccessor.SetBlock(skepemtpyblock.BlockId, blockSel.Position);
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		if (inSlot.Itemstack.Collectible.Attributes != null)
		{
			dsc.AppendLine(Lang.Get("Fill it up with bees and throw it for a stingy surprise"));
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
