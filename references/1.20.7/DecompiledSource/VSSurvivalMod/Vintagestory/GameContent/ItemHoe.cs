using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemHoe : Item
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "hoeInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Block current in api.World.Blocks)
			{
				if (!(current.Code == null) && current.Code.PathStartsWith("soil"))
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-till",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null)
		{
			return;
		}
		if (byEntity.Controls.ShiftKey && byEntity.Controls.CtrlKey)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		BlockPos pos = blockSel.Position;
		Block block = byEntity.World.BlockAccessor.GetBlock(pos);
		if (byEntity.World.BlockAccessor.GetBlock(pos.UpCopy()).Id != 0)
		{
			(api as ICoreClientAPI)?.TriggerIngameError(this, "covered", Lang.Get("Requires no block above"));
			handHandling = EnumHandHandling.PreventDefault;
			return;
		}
		byEntity.Attributes.SetInt("didtill", 0);
		if (block.Code.PathStartsWith("soil"))
		{
			handHandling = EnumHandHandling.PreventDefault;
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null)
		{
			return false;
		}
		if (byEntity.Controls.ShiftKey && byEntity.Controls.CtrlKey)
		{
			return false;
		}
		IPlayer byPlayer = (byEntity as EntityPlayer).Player;
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform tf = new ModelTransform();
			tf.EnsureDefaultValues();
			float rotateToTill = GameMath.Clamp(secondsUsed * 18f, 0f, 2f);
			float scrape = GameMath.SmoothStep(2.5f * GameMath.Clamp(secondsUsed - 0.35f, 0f, 1f));
			float scrapeShake = ((secondsUsed > 0.35f && secondsUsed < 0.75f) ? (GameMath.Sin(secondsUsed * 50f) / 60f) : 0f);
			float rotateWithReset = Math.Max(0f, rotateToTill - GameMath.Clamp(24f * (secondsUsed - 0.75f), 0f, 2f));
			float scrapeWithReset = Math.Max(0f, scrape - Math.Max(0f, 20f * (secondsUsed - 0.75f)));
			tf.Origin.Set(0f, 0f, 0.5f);
			tf.Rotation.Set(0f, rotateWithReset * 45f, 0f);
			tf.Translation.Set(scrapeShake, 0f, scrapeWithReset / 2f);
			byEntity.Controls.UsingHeldItemTransformBefore = tf;
		}
		if (secondsUsed > 0.35f && secondsUsed < 0.87f)
		{
			Vec3d dir = new Vec3d().AheadCopy(1.0, 0f, byEntity.SidedPos.Yaw - (float)Math.PI);
			Vec3d pos = blockSel.Position.ToVec3d().Add(0.5 + dir.X, 1.03, 0.5 + dir.Z);
			pos.X -= dir.X * (double)secondsUsed * 1.0 / 0.75 * 1.2000000476837158;
			pos.Z -= dir.Z * (double)secondsUsed * 1.0 / 0.75 * 1.2000000476837158;
			byEntity.World.SpawnCubeParticles(blockSel.Position, pos, 0.25f, 3, 0.5f, byPlayer);
		}
		if (secondsUsed > 0.6f && byEntity.Attributes.GetInt("didtill") == 0 && byEntity.World.Side == EnumAppSide.Server)
		{
			byEntity.Attributes.SetInt("didtill", 1);
			DoTill(secondsUsed, slot, byEntity, blockSel, entitySel);
		}
		return secondsUsed < 1f;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		return false;
	}

	public virtual void DoTill(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null)
		{
			return;
		}
		BlockPos pos = blockSel.Position;
		Block block = byEntity.World.BlockAccessor.GetBlock(pos);
		if (!block.Code.PathStartsWith("soil"))
		{
			return;
		}
		string fertility = block.LastCodePart(1);
		Block farmland = byEntity.World.GetBlock(new AssetLocation("farmland-dry-" + fertility));
		IPlayer byPlayer = (byEntity as EntityPlayer).Player;
		if (farmland != null && byPlayer != null)
		{
			if (block.Sounds != null)
			{
				byEntity.World.PlaySoundAt(block.Sounds.Place, pos, 0.4);
			}
			byEntity.World.BlockAccessor.SetBlock(farmland.BlockId, pos);
			slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot);
			if (slot.Empty)
			{
				byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.Pos.X, byEntity.Pos.InternalY, byEntity.Pos.Z);
			}
			BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(pos);
			if (be is BlockEntityFarmland)
			{
				((BlockEntityFarmland)be).OnCreatedFromSoil(block);
			}
			byEntity.World.BlockAccessor.MarkBlockDirty(pos);
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
