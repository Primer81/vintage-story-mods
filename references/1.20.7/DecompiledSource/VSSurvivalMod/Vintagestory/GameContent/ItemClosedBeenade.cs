using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemClosedBeenade : Item
{
	protected AssetLocation thrownEntityTypeCode;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		string defaultCode = "thrownbeenade";
		thrownEntityTypeCode = AssetLocation.Create(Attributes?["thrownEntityTypeCode"].AsString(defaultCode) ?? defaultCode, Code.Domain);
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel == null || !(byEntity.World.BlockAccessor.GetBlock(blockSel.Position).FirstCodePart() == "skep"))
		{
			byEntity.Attributes.SetInt("aiming", 1);
			byEntity.AnimManager.StartAnimation("aim");
			handling = EnumHandHandling.PreventDefault;
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform tf = new ModelTransform();
			tf.EnsureDefaultValues();
			float offset = GameMath.Clamp(secondsUsed * 3f, 0f, 2f);
			tf.Translation.Set(offset, (0f - offset) / 4f, 0f);
			byEntity.Controls.UsingHeldItemTransformBefore = tf;
		}
		return true;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.StopAnimation("aim");
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (byEntity.Attributes.GetInt("aiming") == 0)
		{
			return;
		}
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.StopAnimation("aim");
		if (secondsUsed < 0.35f)
		{
			return;
		}
		float damage = 0.5f;
		slot.Itemstack.Collectible.FirstCodePart(1);
		ItemStack stack = slot.TakeOut(1);
		slot.MarkDirty();
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/throw"), byEntity, byPlayer, randomizePitch: false, 8f);
		EntityProperties type = byEntity.World.GetEntityType(thrownEntityTypeCode);
		Entity entity = byEntity.World.ClassRegistry.CreateEntity(type);
		((EntityThrownBeenade)entity).FiredBy = byEntity;
		((EntityThrownBeenade)entity).Damage = damage;
		((EntityThrownBeenade)entity).ProjectileStack = stack;
		float acc = 1f - byEntity.Attributes.GetFloat("aimingAccuracy");
		double rndpitch = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double)acc * 0.75;
		double rndyaw = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double)acc * 0.75;
		Vec3d pos = byEntity.ServerPos.XYZ.Add(0.0, byEntity.LocalEyePos.Y - 0.2, 0.0);
		Vec3d velocity = (pos.AheadCopy(1.0, (double)byEntity.ServerPos.Pitch + rndpitch, (double)byEntity.ServerPos.Yaw + rndyaw) - pos) * 0.5;
		entity.ServerPos.SetPosWithDimension(byEntity.ServerPos.BehindCopy(0.21).XYZ.Add(0.0, byEntity.LocalEyePos.Y - 0.2, 0.0).Ahead(0.25, 0f, byEntity.ServerPos.Yaw + (float)Math.PI / 2f));
		entity.ServerPos.Motion.Set(velocity);
		entity.Pos.SetFrom(entity.ServerPos);
		entity.World = byEntity.World;
		byEntity.World.SpawnEntity(entity);
		byEntity.StartAnimation("throw");
		if (byEntity is EntityPlayer)
		{
			RefillSlotIfEmpty(slot, byEntity, (ItemStack itemstack) => itemstack.Collectible.Code == Code);
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		_ = inSlot.Itemstack.Collectible.Attributes;
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-throw",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
