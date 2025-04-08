using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockBehaviorCreatureContainer : BlockBehavior
{
	public double CreatureSurvivalDays = 1.0;

	private ICoreAPI api;

	private static Dictionary<string, MultiTextureMeshRef> containedMeshrefs = new Dictionary<string, MultiTextureMeshRef>();

	public BlockBehaviorCreatureContainer(Block block)
		: base(block)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		this.api = api;
	}

	public bool HasAnimal(ItemStack itemStack)
	{
		return itemStack.Attributes?.HasAttribute("animalSerialized") ?? false;
	}

	public static double GetStillAliveDays(IWorldAccessor world, ItemStack itemStack)
	{
		return itemStack.Block.GetBehavior<BlockBehaviorCreatureContainer>().CreatureSurvivalDays - (world.Calendar.TotalDays - itemStack.Attributes.GetDouble("totalDaysCaught"));
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		if (HasAnimal(itemstack))
		{
			string shapepath = itemstack.Collectible.Attributes["creatureContainedShape"][itemstack.Attributes.GetString("type")].AsString();
			if (GetStillAliveDays(capi.World, itemstack) > 0.0)
			{
				float escapeSec = itemstack.TempAttributes.GetFloat("triesToEscape") - renderinfo.dt;
				if (api.World.Rand.NextDouble() < 0.001)
				{
					escapeSec = 1f + (float)api.World.Rand.NextDouble() * 2f;
				}
				itemstack.TempAttributes.SetFloat("triesToEscape", escapeSec);
				if (escapeSec > 0f)
				{
					if (api.World.Rand.NextDouble() < 0.05)
					{
						itemstack.TempAttributes.SetFloat("wiggle", 0.05f + (float)api.World.Rand.NextDouble() / 10f);
					}
					float wiggle = itemstack.TempAttributes.GetFloat("wiggle") - renderinfo.dt;
					itemstack.TempAttributes.SetFloat("wiggle", wiggle);
					if (wiggle > 0f)
					{
						shapepath += "-wiggle";
						renderinfo.Transform = renderinfo.Transform.Clone();
						float wiggleX = (float)api.World.Rand.NextDouble() * 4f - 2f;
						float wiggleZ = (float)api.World.Rand.NextDouble() * 4f - 2f;
						if (target != 0)
						{
							wiggleX /= 25f;
							wiggleZ /= 25f;
						}
						if (target == EnumItemRenderTarget.Ground)
						{
							wiggleX /= 4f;
							wiggleZ /= 4f;
						}
						renderinfo.Transform.EnsureDefaultValues();
						renderinfo.Transform.Translation.X += wiggleX;
						renderinfo.Transform.Translation.Z += wiggleZ;
					}
				}
			}
			if (!containedMeshrefs.TryGetValue(shapepath, out var meshref))
			{
				Shape shape = capi.Assets.TryGet(new AssetLocation(shapepath).WithPathPrefix("shapes/").WithPathAppendixOnce(".json")).ToObject<Shape>();
				capi.Tesselator.TesselateShape(block, shape, out var meshdata, new Vec3f(0f, 270f, 0f));
				meshref = (containedMeshrefs[shapepath] = capi.Render.UploadMultiTextureMesh(meshdata));
			}
			renderinfo.ModelRef = meshref;
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		foreach (KeyValuePair<string, MultiTextureMeshRef> containedMeshref in containedMeshrefs)
		{
			containedMeshref.Value.Dispose();
		}
		containedMeshrefs.Clear();
	}

	public override EnumItemStorageFlags GetStorageFlags(ItemStack itemstack, ref EnumHandling handling)
	{
		if (HasAnimal(itemstack))
		{
			handling = EnumHandling.PreventDefault;
			return EnumItemStorageFlags.Backpack;
		}
		return base.GetStorageFlags(itemstack, ref handling);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (HasAnimal(slot.Itemstack))
		{
			handling = EnumHandling.PreventSubsequent;
			if (world.Side == EnumAppSide.Client)
			{
				handling = EnumHandling.PreventSubsequent;
				return false;
			}
			if (!ReleaseCreature(slot, blockSel, byPlayer.Entity))
			{
				failureCode = "creaturenotplaceablehere";
			}
			return false;
		}
		return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref handling, ref failureCode);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		IServerPlayer plr = (byEntity as EntityPlayer).Player as IServerPlayer;
		ICoreServerAPI sapi = api as ICoreServerAPI;
		if (HasAnimal(slot.Itemstack))
		{
			if (blockSel == null)
			{
				base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
				return;
			}
			if (!ReleaseCreature(slot, blockSel, byEntity))
			{
				sapi?.SendIngameError(plr, "nospace", Lang.Get("Not enough space to release animal here"));
			}
			handHandling = EnumHandHandling.PreventDefault;
			handling = EnumHandling.PreventDefault;
			slot.MarkDirty();
		}
		else
		{
			if (entitySel == null)
			{
				return;
			}
			if (!IsCatchable(entitySel.Entity))
			{
				if (!(entitySel.Entity is EntityBoat))
				{
					(byEntity.Api as ICoreClientAPI)?.TriggerIngameError(this, "notcatchable", Lang.Get("This animal is too large, or too wild to catch with a basket"));
				}
				return;
			}
			handHandling = EnumHandHandling.PreventDefault;
			handling = EnumHandling.PreventDefault;
			ItemSlot emptyBackpackSlot = null;
			if (slot is ItemSlotBackpack)
			{
				emptyBackpackSlot = slot;
			}
			else
			{
				IInventory backpackInventory = (byEntity as EntityPlayer)?.Player?.InventoryManager.GetOwnInventory("backpack");
				if (backpackInventory != null)
				{
					emptyBackpackSlot = backpackInventory.Where((ItemSlot slot) => slot is ItemSlotBackpack).FirstOrDefault((ItemSlot slot) => slot.Empty);
				}
			}
			if (emptyBackpackSlot == null)
			{
				sapi?.SendIngameError(plr, "canthold", Lang.Get("Must have empty backpack slot to catch an animal"));
				return;
			}
			ItemStack leftOverBaskets = null;
			if (slot.StackSize > 1)
			{
				leftOverBaskets = slot.TakeOut(slot.StackSize - 1);
			}
			CatchCreature(slot, entitySel.Entity);
			slot.TryFlipWith(emptyBackpackSlot);
			if (slot.Empty)
			{
				slot.Itemstack = leftOverBaskets;
			}
			else if (!byEntity.TryGiveItemStack(leftOverBaskets))
			{
				byEntity.World.SpawnItemEntity(leftOverBaskets, byEntity.ServerPos.XYZ);
			}
			slot.MarkDirty();
			emptyBackpackSlot.MarkDirty();
		}
	}

	private bool IsCatchable(Entity entity)
	{
		JsonObject attributes = entity.Properties.Attributes;
		if (attributes != null && attributes.IsTrue("basketCatchable") && entity.Properties.Attributes["trapChance"].AsFloat() > 0f && entity.WatchedAttributes.GetAsInt("generation") > 4)
		{
			return entity.Alive;
		}
		return false;
	}

	public static void CatchCreature(ItemSlot slot, Entity entity)
	{
		if (entity.World.Side != EnumAppSide.Client)
		{
			ItemStack stack = slot.Itemstack;
			using (MemoryStream ms = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(ms);
				entity.ToBytes(writer, forClient: false);
				stack.Attributes.SetString("classname", entity.Api.ClassRegistry.GetEntityClassName(entity.GetType()));
				stack.Attributes.SetString("creaturecode", entity.Code.ToShortString());
				stack.Attributes.SetBytes("animalSerialized", ms.ToArray());
				double totalDaysReleased = entity.Attributes.GetDouble("totalDaysReleased");
				double catchedDays = totalDaysReleased - entity.Attributes.GetDouble("totalDaysCaught");
				double releasedDays = entity.World.Calendar.TotalDays - totalDaysReleased;
				double unrecoveredDays = Math.Max(0.0, catchedDays - releasedDays * 2.0);
				stack.Attributes.SetDouble("totalDaysCaught", entity.World.Calendar.TotalDays - unrecoveredDays);
			}
			entity.Die(EnumDespawnReason.PickedUp);
		}
	}

	public static bool ReleaseCreature(ItemSlot slot, BlockSelection blockSel, Entity byEntity)
	{
		IWorldAccessor world = byEntity.World;
		if (world.Side == EnumAppSide.Client)
		{
			return true;
		}
		string classname = slot.Itemstack.Attributes.GetString("classname");
		string creaturecode = slot.Itemstack.Attributes.GetString("creaturecode");
		Entity entity = world.Api.ClassRegistry.CreateEntity(classname);
		EntityProperties type2 = world.EntityTypes.FirstOrDefault((EntityProperties type) => type.Code.ToShortString() == creaturecode);
		if (type2 == null)
		{
			return false;
		}
		ItemStack stack = slot.Itemstack;
		using (MemoryStream ms = new MemoryStream(slot.Itemstack.Attributes.GetBytes("animalSerialized")))
		{
			BinaryReader reader = new BinaryReader(ms);
			entity.FromBytes(reader, isSync: false, ((IServerWorldAccessor)world).RemappedEntities);
			Vec3d spawnPos = blockSel.FullPosition;
			Cuboidf collisionBox = type2.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
			if (world.CollisionTester.IsColliding(world.BlockAccessor, collisionBox, spawnPos, alsoCheckTouch: false))
			{
				return false;
			}
			entity.ServerPos.X = (float)(blockSel.Position.X + ((!blockSel.DidOffset) ? blockSel.Face.Normali.X : 0)) + 0.5f;
			entity.ServerPos.Y = blockSel.Position.Y + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Y : 0);
			entity.ServerPos.Z = (float)(blockSel.Position.Z + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Z : 0)) + 0.5f;
			entity.ServerPos.Yaw = (float)world.Rand.NextDouble() * 2f * (float)Math.PI;
			entity.Pos.SetFrom(entity.ServerPos);
			entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
			entity.Attributes.SetString("origin", "playerplaced");
			entity.Attributes.SetDouble("totalDaysCaught", stack.Attributes.GetDouble("totalDaysCaught"));
			entity.Attributes.SetDouble("totalDaysReleased", world.Calendar.TotalDays);
			world.SpawnEntity(entity);
			if (GetStillAliveDays(world, slot.Itemstack) < 0.0)
			{
				(world.Api as ICoreServerAPI).Event.EnqueueMainThreadTask(delegate
				{
					entity.Properties.ResolvedSounds = null;
					entity.Die(EnumDespawnReason.Death, new DamageSource
					{
						CauseEntity = byEntity,
						Type = EnumDamageType.Hunger
					});
				}, "die");
			}
			stack.Attributes.RemoveAttribute("classname");
			stack.Attributes.RemoveAttribute("creaturecode");
			stack.Attributes.RemoveAttribute("animalSerialized");
			stack.Attributes.RemoveAttribute("totalDaysCaught");
		}
		return true;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		AddCreatureInfo(inSlot.Itemstack, dsc, world);
	}

	public void AddCreatureInfo(ItemStack stack, StringBuilder dsc, IWorldAccessor world)
	{
		if (HasAnimal(stack))
		{
			if (GetStillAliveDays(world, stack) > 0.0)
			{
				dsc.AppendLine(Lang.Get("Contains a frightened {0}", Lang.Get("item-creature-" + stack.Attributes.GetString("creaturecode"))));
				dsc.AppendLine(Lang.Get("It remains alive for {0:0.##} more hours", GetStillAliveDays(world, stack) * (double)world.Calendar.HoursPerDay));
			}
			else
			{
				dsc.AppendLine(Lang.Get("Contains a dead {0}", Lang.Get("item-creature-" + stack.Attributes.GetString("creaturecode"))));
			}
		}
	}
}
