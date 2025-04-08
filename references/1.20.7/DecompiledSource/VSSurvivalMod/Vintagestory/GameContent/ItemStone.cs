using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemStone : Item
{
	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
	{
		if (slot.Itemstack?.Collectible == this)
		{
			return "knap";
		}
		return base.GetHeldTpHitAnimation(slot, byEntity);
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockDisplayCase)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			handling = EnumHandHandling.NotHandled;
			return;
		}
		EnumHandHandling bhHandHandling = EnumHandHandling.NotHandled;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling hd = EnumHandling.PassThrough;
			obj.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref bhHandHandling, ref hd);
			if (hd == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		if (bhHandHandling != 0)
		{
			handling = bhHandHandling;
			return;
		}
		bool knappable = itemslot.Itemstack.Collectible.Attributes != null && itemslot.Itemstack.Collectible.Attributes["knappable"].AsBool();
		bool haveKnappableStone = false;
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (byEntity.Controls.ShiftKey && blockSel != null)
		{
			Block block2 = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			haveKnappableStone = block2.Code.PathStartsWith("loosestones") && block2.FirstCodePart(1).Equals(itemslot.Itemstack.Collectible.FirstCodePart(1));
		}
		if (haveKnappableStone)
		{
			if (!knappable)
			{
				if (byEntity.World.Side == EnumAppSide.Client)
				{
					(api as ICoreClientAPI).TriggerIngameError(this, "toosoft", Lang.Get("This type of stone is too soft to be used for knapping."));
				}
				return;
			}
			if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
			{
				itemslot.MarkDirty();
				return;
			}
			IWorldAccessor world = byEntity.World;
			Block knappingBlock = world.GetBlock(new AssetLocation("knappingsurface"));
			if (knappingBlock == null)
			{
				return;
			}
			string failCode = "";
			BlockPos pos = blockSel.Position;
			knappingBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failCode);
			if (failCode == "entityintersecting")
			{
				bool selfBlocked = false;
				string err = ((world.GetIntersectingEntities(pos, knappingBlock.GetCollisionBoxes(world.BlockAccessor, pos), delegate(Entity e)
				{
					selfBlocked = e == byEntity;
					return !(e is EntityItem);
				}).Length == 0) ? Lang.Get("Cannot place a knapping surface here") : (selfBlocked ? Lang.Get("Cannot place a knapping surface here, too close to you") : Lang.Get("Cannot place a knapping surface here, to close to another player or creature.")));
				(api as ICoreClientAPI).TriggerIngameError(this, "cantplace", err);
				return;
			}
			world.BlockAccessor.SetBlock(knappingBlock.BlockId, pos);
			world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
			(api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			if (knappingBlock.Sounds != null)
			{
				world.PlaySoundAt(knappingBlock.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
			}
			if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityKnappingSurface bec)
			{
				bec.BaseMaterial = itemslot.Itemstack.Clone();
				bec.BaseMaterial.StackSize = 1;
				if (byEntity.World is IClientWorldAccessor)
				{
					bec.OpenDialog(world as IClientWorldAccessor, pos, itemslot.Itemstack);
				}
			}
			handling = EnumHandHandling.PreventDefault;
			byEntity.Attributes.SetInt("aimingCancel", 1);
		}
		else if (blockSel != null && byEntity?.World != null && byEntity.Controls.ShiftKey)
		{
			IWorldAccessor world2 = byEntity.World;
			Block block = world2.GetBlock(CodeWithPath("loosestones-" + LastCodePart() + "-free"));
			if (block == null)
			{
				block = world2.GetBlock(CodeWithPath("loosestones-" + LastCodePart(1) + "-" + LastCodePart() + "-free"));
			}
			if (block == null)
			{
				return;
			}
			BlockPos targetpos = blockSel.Position.AddCopy(blockSel.Face);
			targetpos.Y--;
			if (!world2.BlockAccessor.GetMostSolidBlock(targetpos).CanAttachBlockAt(world2.BlockAccessor, block, targetpos, BlockFacing.UP))
			{
				return;
			}
			targetpos.Y++;
			BlockSelection placeSel = blockSel.Clone();
			placeSel.Position = targetpos;
			placeSel.DidOffset = true;
			string error = "";
			if (!block.TryPlaceBlock(world2, byPlayer, itemslot.Itemstack, placeSel, ref error))
			{
				if (api.Side == EnumAppSide.Client)
				{
					(api as ICoreClientAPI).TriggerIngameError(this, "cantplace", Lang.Get("placefailure-" + error));
				}
				return;
			}
			world2.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
			if (block.Sounds != null)
			{
				world2.PlaySoundAt(block.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
			}
			(api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			itemslot.Itemstack.StackSize--;
			handling = EnumHandHandling.PreventDefault;
			byEntity.Attributes.SetInt("aimingCancel", 1);
		}
		else if (!byEntity.Controls.ShiftKey)
		{
			byEntity.Attributes.SetInt("aiming", 1);
			byEntity.Attributes.SetInt("aimingCancel", 0);
			byEntity.StartAnimation("aim");
			handling = EnumHandHandling.PreventDefault;
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		bool result = true;
		bool preventDefault = false;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);
			if (handled != 0)
			{
				result = result && behaviorResult;
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		if (byEntity.Attributes.GetInt("aimingCancel") == 1)
		{
			return false;
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform tf = new ModelTransform();
			tf.EnsureDefaultValues();
			float offset = GameMath.Clamp(secondsUsed * 3f, 0f, 1.5f);
			tf.Translation.Set(offset / 4f, offset / 2f, 0f);
			tf.Rotation.Set(0f, 0f, GameMath.Min(90f, secondsUsed * 360f / 1.5f));
			byEntity.Controls.UsingHeldItemTransformBefore = tf;
		}
		return true;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.StopAnimation("aim");
		if (cancelReason != 0)
		{
			byEntity.Attributes.SetInt("aimingCancel", 1);
		}
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		bool preventDefault = false;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			obj.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);
			if (handled != 0)
			{
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (preventDefault || byEntity.Attributes.GetInt("aimingCancel") == 1)
		{
			return;
		}
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.StopAnimation("aim");
		if (!(secondsUsed < 0.35f))
		{
			float damage = 1f;
			ItemStack stack = slot.TakeOut(1);
			slot.MarkDirty();
			IPlayer byPlayer = null;
			if (byEntity is EntityPlayer)
			{
				byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/throw"), byEntity, byPlayer, randomizePitch: false, 8f);
			EntityProperties type = byEntity.World.GetEntityType(new AssetLocation("thrownstone-" + Variant["rock"]));
			Entity entity = byEntity.World.ClassRegistry.CreateEntity(type);
			((EntityThrownStone)entity).FiredBy = byEntity;
			((EntityThrownStone)entity).Damage = damage;
			((EntityThrownStone)entity).ProjectileStack = stack;
			float acc = 1f - byEntity.Attributes.GetFloat("aimingAccuracy");
			double rndpitch = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double)acc * 0.75;
			double rndyaw = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double)acc * 0.75;
			Vec3d pos = byEntity.ServerPos.XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0);
			Vec3d velocity = (pos.AheadCopy(1.0, (double)byEntity.ServerPos.Pitch + rndpitch, (double)byEntity.ServerPos.Yaw + rndyaw) - pos) * 0.5;
			entity.ServerPos.SetPosWithDimension(byEntity.ServerPos.BehindCopy(0.21).XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0));
			entity.ServerPos.Motion.Set(velocity);
			entity.Pos.SetFrom(entity.ServerPos);
			entity.World = byEntity.World;
			byEntity.World.SpawnEntity(entity);
			byEntity.StartAnimation("throw");
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine(Lang.Get("1 blunt damage when thrown"));
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockKnappingSurface && byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityKnappingSurface bea)
		{
			IPlayer byPlayer = null;
			if (byEntity is EntityPlayer)
			{
				byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (byPlayer != null)
			{
				bea.OnBeginUse(byPlayer, blockSel);
				handling = EnumHandHandling.PreventDefaultAction;
			}
		}
	}

	public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		return false;
	}

	public override bool OnHeldAttackStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		return false;
	}

	public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null || !(byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockKnappingSurface) || !(byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityKnappingSurface bea))
		{
			return;
		}
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (byPlayer != null)
		{
			GetToolMode(slot, byPlayer, blockSel);
			if (byEntity.World is IClientWorldAccessor)
			{
				bea.OnUseOver(byPlayer, blockSel.SelectionBoxIndex, blockSel.Face, mouseMode: true);
			}
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[2]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-throw",
				MouseButton = EnumMouseButton.Right
			},
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-place",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
