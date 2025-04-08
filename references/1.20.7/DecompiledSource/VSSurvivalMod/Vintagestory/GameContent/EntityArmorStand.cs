using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityArmorStand : EntityHumanoid
{
	private EntityBehaviorArmorStandInventory invbh;

	private float fireDamage;

	private string[] poses = new string[4] { "idle", "lefthandup", "righthandup", "twohandscross" };

	public override bool IsCreature => false;

	private int CurPose
	{
		get
		{
			return WatchedAttributes.GetInt("curPose");
		}
		set
		{
			WatchedAttributes.SetInt("curPose", value);
		}
	}

	public override ItemSlot RightHandItemSlot => invbh?.Inventory[15];

	public override ItemSlot LeftHandItemSlot => invbh?.Inventory[16];

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		invbh = GetBehavior<EntityBehaviorArmorStandInventory>();
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
	{
		IPlayer plr = (byEntity as EntityPlayer)?.Player;
		if (plr != null && !byEntity.World.Claims.TryAccess(plr, Pos.AsBlockPos, EnumBlockAccessFlags.Use))
		{
			plr.InventoryManager.ActiveHotbarSlot.MarkDirty();
			WatchedAttributes.MarkAllDirty();
			return;
		}
		if (mode == EnumInteractMode.Interact && byEntity.RightHandItemSlot?.Itemstack?.Collectible is ItemWrench)
		{
			AnimManager.StopAnimation(poses[CurPose]);
			CurPose = (CurPose + 1) % poses.Length;
			AnimManager.StartAnimation(new AnimationMetaData
			{
				Animation = poses[CurPose],
				Code = poses[CurPose]
			}.Init());
			return;
		}
		ItemSlot handslot;
		if (mode == EnumInteractMode.Interact && byEntity.RightHandItemSlot != null)
		{
			handslot = byEntity.RightHandItemSlot;
			if (handslot.Empty)
			{
				for (int j = 0; j < invbh.Inventory.Count; j++)
				{
					ItemSlot gslot2 = invbh.Inventory[j];
					if (!gslot2.Empty)
					{
						if (gslot2.Itemstack.Collectible?.Code == null)
						{
							gslot2.Itemstack = null;
						}
						else if (gslot2.TryPutInto(byEntity.World, handslot) > 0)
						{
							byEntity.World.Logger.Audit("{0} Took 1x{1} from Armor Stand at {2}.", byEntity.GetName(), handslot.Itemstack.Collectible.Code, ServerPos.AsBlockPos);
							return;
						}
					}
				}
				goto IL_02db;
			}
			if (!slot.Itemstack.Collectible.Tool.HasValue)
			{
				JsonObject itemAttributes = slot.Itemstack.ItemAttributes;
				if (itemAttributes == null || !itemAttributes["toolrackTransform"].Exists)
				{
					if (!ItemSlotCharacter.IsDressType(slot.Itemstack, EnumCharacterDressType.ArmorBody) && !ItemSlotCharacter.IsDressType(slot.Itemstack, EnumCharacterDressType.ArmorHead) && !ItemSlotCharacter.IsDressType(slot.Itemstack, EnumCharacterDressType.ArmorLegs))
					{
						(byEntity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", "Cannot place dresses or other non-armor items on armor stands");
						return;
					}
					goto IL_02db;
				}
			}
			AssetLocation collectibleCode2 = handslot.Itemstack.Collectible.Code;
			if (handslot.TryPutInto(byEntity.World, RightHandItemSlot) == 0)
			{
				handslot.TryPutInto(byEntity.World, LeftHandItemSlot);
			}
			byEntity.World.Logger.Audit("{0} Put 1x{1} onto Armor Stand at {2}.", byEntity.GetName(), collectibleCode2, ServerPos.AsBlockPos);
			return;
		}
		goto IL_043e;
		IL_043e:
		if (Alive && World.Side != EnumAppSide.Client && mode != 0)
		{
			base.OnInteract(byEntity, slot, hitPosition, mode);
		}
		return;
		IL_02db:
		WeightedSlot sinkslot = invbh.Inventory.GetBestSuitedSlot(handslot);
		if (sinkslot.weight > 0f && sinkslot.slot != null)
		{
			AssetLocation collectibleCode = handslot.Itemstack.Collectible.Code;
			handslot.TryPutInto(byEntity.World, sinkslot.slot);
			byEntity.World.Logger.Audit("{0} Put 1x{1} onto Armor Stand at {2}.", byEntity.GetName(), collectibleCode, ServerPos.AsBlockPos);
			return;
		}
		bool empty = true;
		for (int i = 0; i < invbh.Inventory.Count; i++)
		{
			ItemSlot gslot = invbh.Inventory[i];
			empty &= gslot.Empty;
		}
		if (empty && byEntity.Controls.ShiftKey)
		{
			ItemStack stack = new ItemStack(byEntity.World.GetItem(Code));
			if (!byEntity.TryGiveItemStack(stack))
			{
				byEntity.World.SpawnItemEntity(stack, ServerPos.XYZ);
			}
			byEntity.World.Logger.Audit("{0} Took 1x{1} from Armor Stand at {2}.", byEntity.GetName(), stack.Collectible.Code, ServerPos.AsBlockPos);
			Die();
			return;
		}
		goto IL_043e;
	}

	public override bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		if (damageSource.Source == EnumDamageSource.Internal && damageSource.Type == EnumDamageType.Fire)
		{
			fireDamage += damage;
		}
		if (fireDamage > 4f)
		{
			Die();
		}
		return base.ReceiveDamage(damageSource, damage);
	}
}
